using MyLab.Log.Dsl;
using MyLab.Log.Scopes;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools;

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
        private readonly LogContainerRegistry _registry = new();
        private readonly IDslLogger? _log;

        private readonly ILogFormat _defaultLogFormat = new DefaultLogFormat();

        private readonly IDictionary<string, ILogFormat> _logFormats = new Dictionary<string, ILogFormat>
        {

        };

        private readonly ILogRegistrar _logRegistrar;
        private readonly IContextPropertiesProvider _contextPropertiesProvider;

        public DockerLogMonitor(IDockerContainerProvider containerProvider, 
            IDockerContainerFilesProvider containerFilesProvider, 
            ILogRegistrar logRegistrar,
            IContextPropertiesProvider contextPropertiesProvider,
            ILogger<DockerLogMonitor>? logger = null)
        {
            _logRegistrar = logRegistrar;
            _contextPropertiesProvider = contextPropertiesProvider ?? throw new ArgumentNullException(nameof(contextPropertiesProvider));
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
            _containerFilesProvider = containerFilesProvider ?? throw new ArgumentNullException(nameof(containerFilesProvider));
            _log = logger?.Dsl();
        }

        public async Task ProcessLogsAsync(CancellationToken cancellationToken)
        {
            var actualContainers = await _containerProvider.ProvideContainersAsync(cancellationToken);

            var syncReport = _registry.Sync(actualContainers);

            if (_log != null && syncReport != LogContainerRegistry.SyncReport.Empty)
            {
                _log.Action("Docker container list changed")
                    .AndFactIs("Removed", syncReport.Removed.Count == 0
                        ? "[empty]"
                        : string.Join(", ", syncReport.Removed.Select(c => c.Name))
                    )
                    .AndFactIs("Added", syncReport.Added.Count == 0
                        ? "[empty]"
                        : string.Join(", ", syncReport.Added.Select(c => c.Name))
                    )
                    .Write();
            }

            foreach (var container in _registry)
            {
                container.LastIterationDt = DateTime.Now;

                using var scope = _log?.BeginScope(new LabelLogScope("scoped-container", container.Container.Name));

                try
                {
                    await ProcessContainerLogs(container, cancellationToken);
                }
                catch (Exception e)
                {
                    container.LastError = e;
                    _log?.Error(e).Write();
                }
            }
        }

        private async Task ProcessContainerLogs(LogContainerRegistry.Entity cEntity, CancellationToken cancellationToken)
        {
            ILogFormat? format;

            if (cEntity.Container.LogFormat == null)
                format = _defaultLogFormat;
            else
            {
                if (!_logFormats.TryGetValue(cEntity.Container.LogFormat, out format))
                {
                    _log?.Error("Log format is not supported")
                        .AndFactIs("format", cEntity.Container.LogFormat)
                        .Write();
                    return;
                }
            }
            
            var lastLogFilename = GetLastLogFilename(cEntity);
            if (lastLogFilename == null) return;

            if (lastLogFilename != cEntity.LastLogFilename)
            {
                cEntity.LastLogFilename = lastLogFilename;
                cEntity.Shift = 0;
            }

            using var fileReader = _containerFilesProvider.OpenContainerFileRead(cEntity.Container.Id, lastLogFilename);
            //fileReader.BaseStream.Seek(cEntity.Shift, SeekOrigin.Begin);
            fileReader.BaseStream.Position = cEntity.Shift;

            var srcReader = new DockerLogSourceReader(fileReader);

            var logReader = new LogReader(format, srcReader, cEntity.LineBuff);
            
            while (await logReader.ReadLogAsync(cancellationToken) is { } nextLogRecord)
            {
                if (nextLogRecord.Properties != null)
                {
                    nextLogRecord.Properties.AddRange(_contextPropertiesProvider.ProvideProperties());
                }
                else
                {
                    nextLogRecord.Properties =
                        new List<LogProperty>(_contextPropertiesProvider.ProvideProperties());
                }
                await _logRegistrar.RegisterAsync(nextLogRecord);
            }

            cEntity.Shift = fileReader.BaseStream.Position;
            await _logRegistrar.FlushAsync();
        }
        
        private string? GetLastLogFilename(LogContainerRegistry.Entity cEntity)
        {
            var foundLogFiles = _containerFilesProvider
                .EnumerateContainerFiles(cEntity.Container.Id)
                .Where(f => LogFileSelector.Predicate(cEntity.Container.Id, f));

            var lastLogFilename = foundLogFiles.MaxBy(f => f, new LogFilenameComparer());
            return lastLogFilename;
        }
    }
}
