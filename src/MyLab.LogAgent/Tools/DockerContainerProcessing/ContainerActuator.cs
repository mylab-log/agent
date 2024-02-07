using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Services;
using MyLab.Log.Dsl;

namespace MyLab.LogAgent.Tools.DockerContainerProcessing
{
    public class ContainerActuator
    {
        private readonly IDockerContainerRegistry _containerRegistry;
        public IMetricsOperator? MetricsOperator { get; set; }
        public IDslLogger? Logger { get; set; }

        public ContainerActuator(IDockerContainerRegistry containerRegistry)
        {
            _containerRegistry = containerRegistry ?? throw new ArgumentNullException(nameof(containerRegistry));
        }

        public async Task ActuateAsync(IDockerContainerProvider containerProvider, CancellationToken cancellationToken)
        {
            if (containerProvider == null) throw new ArgumentNullException(nameof(containerProvider));

            var actualContainers = await containerProvider.ProvideContainersAsync(cancellationToken);

            var syncReport = _containerRegistry.Sync(actualContainers);

            ProcessContainerSyncReport(syncReport);
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

            MetricsOperator?.RegisterContainerSyncReport(syncReport);

            if (Logger != null)
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

                Logger.Action("Docker container list changed")
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

        private bool ProvideFormat(DockerContainerMonitoringState cEntity, out ILogFormat format, out string formatName)
        {
            bool unsupportedFormat = false;

            if (cEntity.Info.LogFormat == null)
            {
                format = SupportedLogFormats.Default;
                formatName = "default";
            }
            else
            {

                if (SupportedLogFormats.Instance.TryGetValue(cEntity.Info.LogFormat, out var foundFormat))
                {
                    format = foundFormat;
                }
                else
                {
                    Logger?.Warning("Log format is not supported")
                        .AndFactIs("format", cEntity.Info.LogFormat ?? "[null]")
                        .Write();
                    format = SupportedLogFormats.Default;
                    unsupportedFormat = true;
                }

                formatName = cEntity.Info.LogFormat!;
            }

            Logger?.Debug("Log format detected")
                .AndFactIs("format", cEntity.Info.LogFormat)
                .Write();

            return !unsupportedFormat;
        }
    }
}
