using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;
using MyLab.LogAgent.Tools;
using MyLab.LogAgent.Tools.LogMessageExtraction;

namespace MyLab.LogAgent.Services;

public interface IContainerMonitoringProcessor
{
    Task ProcessAsync(DockerContainerMonitoringState cState, CancellationToken cancellationToken);
}

class ContainerMonitoringProcessor : IContainerMonitoringProcessor
{
    private readonly IDockerContainerFilesProvider _containerFilesProvider;
    private readonly ILogRegistrar _logRegistrar;
    private readonly IMetricsOperator? _metricsOperator;
    private readonly IDslLogger? _log;
    private readonly LogMessageExtractor _logMessageExtractor;
    private readonly LogAgentOptions _opts;
    private readonly LabelFilter _labelFilter;

    public ContainerMonitoringProcessor(
        IDockerContainerFilesProvider containerFilesProvider,
        ILogRegistrar logRegistrar,
        IMetricsOperator? metricsOperator,
        IOptions<LogAgentOptions> opts,
        ILogger<ContainerMonitoringProcessor>? logger)
    {
        _containerFilesProvider = containerFilesProvider ?? throw new ArgumentNullException(nameof(containerFilesProvider));
        _logRegistrar = logRegistrar ?? throw new ArgumentNullException(nameof(logRegistrar));
        _metricsOperator = metricsOperator;

        _log = logger.Dsl();
        _opts = opts.Value;
        _logMessageExtractor = new LogMessageExtractor(_opts.MessageLenLimit);

        _labelFilter = new(
            opts.Value.Docker.WhiteLabels ?? ["*"],
            opts.Value.Docker.BlackLabels,
            new[]
            {
                "net.mylab.*",
                "com.docker.*",
                "io.docker.*",
                "org.dockerproject.*",
                "com.docker.compose.*"
            });
    }
    
    private void ApplyAddProps(LogRecord nextLogRecord)
    {
        if (_opts.AddProperties != null)
        {
            var logProps = _opts.AddProperties.Select(p => new KeyValuePair<string, object>(p.Key, p.Value));
            if (nextLogRecord.Properties != null)
            {
                nextLogRecord.Properties.AddRange(logProps);
            }
            else
            {
                nextLogRecord.Properties = new LogProperties(new Dictionary<string, object>(logProps));
            }
        }
    }

    public async Task ProcessAsync(DockerContainerMonitoringState cState, CancellationToken cancellationToken)
    {
        var lastLogFile = _containerFilesProvider.GetActualContainerLogFile(cState.Info.Id);

        if (lastLogFile == null)
        {
            _log?.Warning("Can't detect log filename")
                .Write();

            return;
        }

        if (cState.LastIteration.Filename != null)
        {
            if (lastLogFile.Filename != cState.LastIteration.Filename)
            {
                _log?.Debug("Switch to new log filename")
                    .AndFactIs("old-filename", cState.LastIteration.Filename)
                    .AndFactIs("new-filename", lastLogFile)
                    .Write();

                cState.LastIteration.Filename = lastLogFile.Filename;
                cState.Shift = 0;
            }
            else if(lastLogFile.Length < cState.Shift)
            {
                //File has been reset
                cState.Shift = 0;
            }
        }
        else
        {
            var initialShift = _opts.ReadFromEnd ? lastLogFile.Length : 0;

            cState.LastIteration.Filename = lastLogFile.Filename;
            cState.Shift = initialShift;

            _log?.Debug("Initial monitoring detected")
                .AndFactIs("initial-filename", lastLogFile)
                .AndFactIs("initial-shift", initialShift)
                .AndFactIs("read-from-end", _opts.ReadFromEnd)
                .Write();

            if (_opts.ReadFromEnd)
                return;
        }

        using var fileReader = _containerFilesProvider.OpenContainerFileRead(cState.Info.Id, lastLogFile.Filename);
        fileReader.BaseStream.Position = cState.Shift;

        var srcReader = new DockerLogSourceReader(fileReader)
        {
            IgnoreStreamType = cState.Info.IgnoreStreamType
        };

        if (cState.Format == null)
            throw new InvalidOperationException("Null format");

        var logReader = new LogReader(cState.Format, _logMessageExtractor, srcReader)
        {
            Buffer = cState.LineBuff,
            UseSourceDt = _opts.UseSourceDt
        };

        int recordCount = 0;

        while (await logReader.ReadLogAsync(cancellationToken) is { } nextLogRecord)
        {
            nextLogRecord.Format = cState.FormatName;
            nextLogRecord.Container = cState.Info.Name;

            ApplyAddProps(nextLogRecord);

            if (cState.UnsupportedFormatDetected)
            {
                nextLogRecord.Properties ??= new LogProperties();
                nextLogRecord.Properties.Add(LogPropertyNames.UnsupportedFormatFlag, "true");
            }

            TryAddContainerLabels(nextLogRecord, cState.Info.Labels);

            await _logRegistrar.RegisterAsync(nextLogRecord, cancellationToken);

            recordCount++;

            _metricsOperator?.RegisterLogReading(nextLogRecord);

            if(nextLogRecord.HasParsingError)
            {
                _log?.Warning("Log parsing error")
                    .AndFactIs("record", nextLogRecord)
                    .Write();
            }
        }

        cState.LastIteration.LogCount = recordCount;
        cState.Shift = fileReader.BaseStream.Position;
        await _logRegistrar.FlushAsync(cancellationToken);

        _log?.Debug("Log file reading completion")
            .AndFactIs("filename", lastLogFile)
            .AndFactIs("new-shift", cState.Shift)
            .AndFactIs("rec-count", recordCount)
            .Write();
    }

    private void TryAddContainerLabels(LogRecord rec, IReadOnlyDictionary<string, string>? labels)
    {
        if(labels == null || labels.Count == 0) return;

        var lblDict = new Dictionary<string, string>
        (
            labels.Where(kv => _labelFilter.IsMatch(kv.Key))
        );

        if(lblDict.Count != 0)
        {
            rec.Properties ??= new LogProperties();
            rec.Properties.Add(LogPropertyNames.ContainerLabels, lblDict);
        }
    }
}