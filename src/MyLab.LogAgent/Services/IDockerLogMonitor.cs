using MyLab.Log.Dsl;
using MyLab.LogAgent.Tools.DockerContainerProcessing;

namespace MyLab.LogAgent.Services
{
    public interface IDockerLogMonitor
    {
        Task ProcessLogsAsync(CancellationToken cancellationToken);
    }

    class DockerLogMonitor : IDockerLogMonitor
    {
        private readonly IDockerContainerProvider _containerProvider;
        private readonly IDockerContainerRegistry _containerRegistry;
        private readonly IDslLogger? _log;
        private readonly IMetricsOperator? _metricsOperator;
        private readonly IContainerMonitoringProcessor _containerMonitoringProcessor;

        public DockerLogMonitor(
            IDockerContainerProvider containerProvider, 
            IDockerContainerRegistry containerRegistry,
            IContainerMonitoringProcessor containerMonitoringProcessor,
            IMetricsOperator? metricsOperator = null,
            ILogger<DockerLogMonitor>? logger = null)
        {
            _metricsOperator = metricsOperator;
            _containerMonitoringProcessor = containerMonitoringProcessor ?? throw new ArgumentNullException(nameof(containerMonitoringProcessor));
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
            _containerRegistry = containerRegistry ?? throw new ArgumentNullException(nameof(containerRegistry));
            _log = logger?.Dsl();
        }

        public async Task ProcessLogsAsync(CancellationToken cancellationToken)
        {
            var cActuator = new ContainerActuator(_containerRegistry)
            {
                Logger = _log,
                MetricsOperator = _metricsOperator
            };

            await cActuator.ActuateAsync(_containerProvider, cancellationToken);
            
            var cIterator = new ContainerProcessingIterator(_containerRegistry)
            {
                Logger = _log
            };

            await cIterator.IterateAsync(_containerMonitoringProcessor, cancellationToken);
        }
    }
}
