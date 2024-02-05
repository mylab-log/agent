using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.Log.Scopes;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;
using MyLab.LogAgent.Tools;
using MyLab.LogAgent.Tools.LogMessageExtraction;

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
                { "mylab-yaml", new MyLabLogFormat() },
                { "net", new NetLogFormat() },
                { "net+mylab", new NetMyLabLogFormat() },
                { "nginx", new NginxLogFormat() }
            };
        }

        public async Task ProcessLogsAsync(CancellationToken cancellationToken)
        {
            var actualContainers = await _containerProvider.ProvideContainersAsync(cancellationToken);

            var syncReport = _containerRegistry.Sync(actualContainers);

            ProcessContainerSyncReport(syncReport);

            foreach (var container in _containerRegistry.GetContainers().Where(c => c.Info.Enabled))
            {
                container.LastIteration.DateTime = DateTime.Now;

                using var scope = _log?.BeginScope(new LabelLogScope("scoped-container", container.Info.Name));

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

        private void ProcessContainerSyncReport(DockerContainerSyncReport syncReport)
        {
            if (syncReport == DockerContainerSyncReport.Empty)
                return;

            foreach (var cEntity in syncReport.Added)
            {
                bool unsupportedFormat = !ProvideFormat(cEntity, out var format, out var formatName);

                cEntity.Format = format;
                cEntity.FormatName = formatName;
                cEntity.UnsupportedFormatDetected = unsupportedFormat;
            }

            LogAgentMetrics.UpdateContainerMetrics(syncReport);

            if (_log != null)
            {
                var removedList = syncReport.Removed
                    .Select(c => c.Info.Name)
                    .ToArray();

                var addedList = syncReport.Added
                    .Where(c => c.Info.Enabled)
                    .Select(c => c.Info.Name)
                    .ToArray();

                var disabledList = syncReport.Added
                    .Where(c => !c.Info.Enabled)
                    .Select(c => c.Info.Name)
                    .ToArray();

                _log.Action("Docker container list changed")
                    .AndFactIs("removed", removedList.Length == 0
                        ? "[empty]"
                        : string.Join(", ", removedList)
                    )
                    .AndFactIs("added", addedList.Length == 0
                        ? "[empty]"
                        : string.Join(", ", addedList)
                    )
                    .AndFactIs("disabled", disabledList.Length == 0
                        ? "[empty]"
                        : string.Join(", ", disabledList)
                    )
                    .Write();
            }
        }

        private async Task ProcessContainerLogs(DockerContainerMonitoringState cEntity, CancellationToken cancellationToken)
        {
            var lastLogFile = GetLastLogFilename(cEntity.Info.Id);

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

            using var fileReader = _containerFilesProvider.OpenContainerFileRead(cEntity.Info.Id, lastLogFile.Filename);
            fileReader.BaseStream.Position = cEntity.Shift;

            var srcReader = new DockerLogSourceReader(fileReader)
            {
                IgnoreStreamType = cEntity.Info.IgnoreStreamType
            };

            if (cEntity.Format == null)
                throw new InvalidOperationException("Null format");

            var logReader = new LogReader(cEntity.Format, _logMessageExtractor, srcReader)
            {
                Buffer = cEntity.LineBuff
            };
            
            int recordCount = 0;

            while (await logReader.ReadLogAsync(cancellationToken) is { } nextLogRecord)
            {
                nextLogRecord.Format = cEntity.FormatName;
                nextLogRecord.Container = cEntity.Info.Name;

                ApplyAddProps(nextLogRecord);

                if (cEntity.UnsupportedFormatDetected)
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

                LogAgentMetrics.UpdateReadingMetrics(nextLogRecord);
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

        private bool ProvideFormat(DockerContainerMonitoringState cEntity, out ILogFormat format, out string formatName)
        {
            bool unsupportedFormat = false;

            if (cEntity.Info.LogFormat == null)
            {
                format = _defaultLogFormat;
                formatName = "default";
            }
            else
            {

                if (_logFormats.TryGetValue(cEntity.Info.LogFormat, out var foundFormat))
                {
                    format = foundFormat;
                }
                else
                {
                    _log?.Warning("Log format is not supported")
                        .AndFactIs("format", cEntity.Info.LogFormat ?? "[null]")
                        .Write();
                    format = _defaultLogFormat;
                    unsupportedFormat = true;
                }

                formatName = cEntity.Info.LogFormat!;
            }

            _log?.Debug("Log format detected")
                .AndFactIs("format", cEntity.Info.LogFormat)
                .Write();

            return !unsupportedFormat;
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
