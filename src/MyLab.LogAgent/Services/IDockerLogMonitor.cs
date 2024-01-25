﻿using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.Log.Scopes;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;
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
        private readonly LogAgentOptions _opts;

        public DockerLogMonitor(IDockerContainerProvider containerProvider, 
            IDockerContainerFilesProvider containerFilesProvider, 
            ILogRegistrar logRegistrar,
            IOptions<LogAgentOptions> opts,
            ILogger<DockerLogMonitor>? logger = null)
        {
            _logRegistrar = logRegistrar;
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
            _containerFilesProvider = containerFilesProvider ?? throw new ArgumentNullException(nameof(containerFilesProvider));
            _opts = opts.Value ?? throw new ArgumentException("Options is not defined", nameof(opts));
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

                _log?.Debug("Container monitoring").Write();

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

            _log?.Debug("Log format detected")
                .AndFactIs("format", format)
                .Write();

            var lastLogFilename = GetLastLogFilename(cEntity);

            if (lastLogFilename == null)
            {
                _log?.Debug("Can't detect log filename")
                    .Write();

                return;
            }

            if (lastLogFilename != cEntity.LastLogFilename)
            {
                cEntity.LastLogFilename = lastLogFilename;
                cEntity.Shift = 0;
            }

            using var fileReader = _containerFilesProvider.OpenContainerFileRead(cEntity.Container.Id, lastLogFilename);
            fileReader.BaseStream.Position = cEntity.Shift;

            var srcReader = new DockerLogSourceReader(fileReader);

            var logReader = new LogReader(format, srcReader, cEntity.LineBuff);

            _log?.Debug("Try to read log file")
                .AndFactIs("filename", lastLogFilename)
                .AndFactIs("shift", cEntity.Shift)
                .Write();

            int recordCount = 0;

            while (await logReader.ReadLogAsync(cancellationToken) is { } nextLogRecord)
            {
                ApplyAddProps(nextLogRecord);

                await _logRegistrar.RegisterAsync(nextLogRecord);

                recordCount++;
            }

            cEntity.Shift = fileReader.BaseStream.Position;
            await _logRegistrar.FlushAsync();

            _log?.Debug("Log file reading completion")
                .AndFactIs("filename", lastLogFilename)
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