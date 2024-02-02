using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.Log.Scopes;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;
using MyLab.LogAgent.Tools;
using MyLab.LogAgent.Tools.LogMessageProc;

namespace MyLab.LogAgent.Services
{
    public interface IDockerLogMonitor
    {
        Task ProcessLogsAsync(CancellationToken cancellationToken);
    }

    class DockerLogMonitor : IDockerLogMonitor
    {
        private readonly IDockerContainerProvider _containerProvider;
        private readonly IDockerContainerFilesProvider _containerFilesProvider;
        private readonly IDockerContainerRegistry _containerRegistry;
        private readonly IDslLogger? _log;

        private readonly ILogFormat _defaultLogFormat = new DefaultLogFormat();

        private readonly IDictionary<string, ILogFormat> _logFormats;

        private readonly ILogRegistrar _logRegistrar;
        private readonly LogAgentOptions _opts;
        private readonly LogMessageExtractor _logMessageExtractor;

        public DockerLogMonitor(IDockerContainerProvider containerProvider, 
            IDockerContainerFilesProvider containerFilesProvider,
            IDockerContainerRegistry containerRegistry,
            ILogRegistrar logRegistrar,
            IOptions<LogAgentOptions> opts,
            ILogger<DockerLogMonitor>? logger = null)
        {
            _logRegistrar = logRegistrar;
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
            _containerFilesProvider = containerFilesProvider ?? throw new ArgumentNullException(nameof(containerFilesProvider));
            _containerRegistry = containerRegistry ?? throw new ArgumentNullException(nameof(containerRegistry));
            _opts = opts.Value ?? throw new ArgumentException("Options is not defined", nameof(opts));
            _log = logger?.Dsl();

            _logMessageExtractor = new LogMessageExtractor(_opts.MessageLenLimit);

            _logFormats = new Dictionary<string, ILogFormat>
            {
                { "default", new DefaultLogFormat() },
                { "mylab", new MyLabLogFormat() },
                { "net", new NetLogFormat() },
                { "net+mylab", new NetMyLabLogFormat() },
                { "nginx", new NginxLogFormat() }
            };
        }

        public async Task ProcessLogsAsync(CancellationToken cancellationToken)
        {
            var actualContainers = await _containerProvider.ProvideContainersAsync(cancellationToken);

            var syncReport = _containerRegistry.Sync(actualContainers);

            if (_log != null && syncReport != DockerContainerSyncReport.Empty)
            {
                _log.Action("Docker container list changed")
                    .AndFactIs("removed", syncReport.Removed.Count == 0
                        ? "[empty]"
                        : string.Join(", ", syncReport.Removed.Select(c => c.Name))
                    )
                    .AndFactIs("added", syncReport.Added.Count == 0
                        ? "[empty]"
                        : string.Join(", ", syncReport.Added.Select(c => c.Name))
                    )
                    .Write();
            }

            foreach (var container in _containerRegistry.GetContainers())
            {
                container.LastIteration.DateTime = DateTime.Now;

                using var scope = _log?.BeginScope(new LabelLogScope("scoped-container", container.Container.Name));

                try
                {
                    await ProcessContainerLogs(container, cancellationToken);
                }
                catch (Exception e)
                {
                    container.LastIteration.Error = e;
                    _log?.Error(e).Write();
                }
            }
        }

        private async Task ProcessContainerLogs(DockerContainerMonitoringState cEntity, CancellationToken cancellationToken)
        {
            ILogFormat? format;
            string formatName;
            bool unsupportedFormat = false;

            if (cEntity.Container.LogFormat == null)
            {
                format = _defaultLogFormat;
                formatName = "default";
            }
            else
            {
                if (!_logFormats.TryGetValue(cEntity.Container.LogFormat, out format))
                {
                    _log?.Warning("Log format is not supported")
                        .AndFactIs("format", cEntity.Container.LogFormat)
                        .Write();
                    format = _defaultLogFormat;
                    unsupportedFormat = true;
                }

                formatName = cEntity.Container.LogFormat;
            }

            _log?.Debug("Log format detected")
                .AndFactIs("format", cEntity.Container.LogFormat)
                .Write();

            var lastLogFile = GetLastLogFilename(cEntity.Container.Id);

            if (lastLogFile == null)
            {
                _log?.Warning("Can't detect log filename")
                    .Write();

                return;
            }

            if (cEntity.LastIteration.Filename != null)
            {
                if (lastLogFile.Filename != cEntity.LastIteration.Filename)
                {
                    _log?.Debug("Switch to new log filename")
                        .AndFactIs("old-filename", cEntity.LastIteration.Filename)
                        .AndFactIs("new-filename", lastLogFile)
                        .Write();

                    cEntity.LastIteration.Filename = lastLogFile.Filename;
                    cEntity.Shift = 0;
                }
            }
            else
            {
                var initialShift = _opts.ReadFromEnd ? lastLogFile.Length : 0;

                cEntity.LastIteration.Filename = lastLogFile.Filename;
                cEntity.Shift = initialShift;

                _log?.Debug("Initial monitoring detected")
                    .AndFactIs("initial-filename", lastLogFile)
                    .AndFactIs("initial-shift", initialShift)
                    .AndFactIs("read-from-end", _opts.ReadFromEnd)
                    .Write();

                if(_opts.ReadFromEnd)
                    return;
            }

            using var fileReader = _containerFilesProvider.OpenContainerFileRead(cEntity.Container.Id, lastLogFile.Filename);
            fileReader.BaseStream.Position = cEntity.Shift;

            var srcReader = new DockerLogSourceReader(fileReader)
            {
                IgnoreStreamType = cEntity.Container.IgnoreStreamType
            };

            var logReader = new LogReader(format, _logMessageExtractor, srcReader, cEntity.LineBuff);
            
            int recordCount = 0;

            while (await logReader.ReadLogAsync(cancellationToken) is { } nextLogRecord)
            {
                nextLogRecord.Format = formatName;
                nextLogRecord.Container = cEntity.Container.Name;

                ApplyAddProps(nextLogRecord);

                if (unsupportedFormat)
                {
                    nextLogRecord.Properties ??= [];
                    nextLogRecord.Properties.Add(new LogProperty
                    {
                        Name = LogPropertyNames.UnsupportedFormatFlag,
                        Value = "true"
                    });
                }

                await _logRegistrar.RegisterAsync(nextLogRecord);

                recordCount++;
            }

            cEntity.LastIteration.LogCount = recordCount;
            cEntity.Shift = fileReader.BaseStream.Position;
            await _logRegistrar.FlushAsync();

            _log?.Debug("Log file reading completion")
                .AndFactIs("filename", lastLogFile)
                .AndFactIs("new-shift", cEntity.Shift)
                .AndFactIs("rec-count", recordCount)
                .Write();
        }

        private void ApplyAddProps(LogRecord nextLogRecord)
        {
            if (_opts.AddProperties != null)
            {
                var logProps = _opts.AddProperties.Select(p => new LogProperty { Name = p.Key, Value = p.Value });
                if (nextLogRecord.Properties != null)
                {
                    nextLogRecord.Properties.AddRange(logProps);
                }
                else
                {
                    nextLogRecord.Properties = new List<LogProperty>(logProps);
                }
            }
        }

        private ContainerFile? GetLastLogFilename(string containerId)
        {
            var foundLogFiles = _containerFilesProvider
                .EnumerateContainerFiles(containerId)
                .Where(f => LogFileSelector.Predicate(containerId, f.Filename));

            var lastLogFilename = foundLogFiles.MaxBy(f => f.Filename, new LogFilenameComparer());
            return lastLogFilename;
        }
    }
}
