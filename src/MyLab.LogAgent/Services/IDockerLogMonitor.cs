using MyLab.Log.Dsl;
using MyLab.Log.Scopes;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.Tools;

namespace MyLab.LogAgent.Services
{
    public interface IDockerLogMonitor
    {
        Task ProcessLogsAsync(CancellationToken cancellationToken);
    }

    class DockerLogMonitor
        (
            IDockerContainerProvider containerProvider, 
            IDockerContainerFilesProvider containerFilesProvider, 
            ILogRegistrar logRegistrar,
            ILogger<DockerLogMonitor>? logger = null
        ) : IDockerLogMonitor
    {
        private readonly IDockerContainerProvider _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
        private readonly IDockerContainerFilesProvider _containerFilesProvider = containerFilesProvider ?? throw new ArgumentNullException(nameof(containerFilesProvider));
        private readonly LogContainerRegistry _registry = new();
        private readonly IDslLogger? _log = logger?.Dsl();

        private readonly ILogFormat _defaultLogFormat = new DefaultLogFormat();

        private readonly IDictionary<string, ILogFormat> _logFormats = new Dictionary<string, ILogFormat>
        {

        };

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
            fileReader.BaseStream.Seek(cEntity.Shift, SeekOrigin.Begin);

            var logReader = new LogReader(format, fileReader, cEntity.LineBuff);

            while (await logReader.ReadLogAsync(cancellationToken) is { } nextLogRecord)
            {
                await logRegistrar.RegisterAsync(nextLogRecord);
            }

            cEntity.Shift = fileReader.BaseStream.Position;
            await logRegistrar.FlushAsync();
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
