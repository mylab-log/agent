using MyLab.Log.Scopes;
using MyLab.LogAgent.Services;
using System.Threading;
using MyLab.Log.Dsl;

namespace MyLab.LogAgent.Tools.DockerContainerProcessing
{
    public class ContainerProcessingIterator
    {
        private readonly IDockerContainerRegistry _containerRegistry;

        public IDslLogger? Logger { get; set; }

        public ContainerProcessingIterator(IDockerContainerRegistry containerRegistry)
        {
            _containerRegistry = containerRegistry ?? throw new ArgumentNullException(nameof(containerRegistry));
        }

        public async Task IterateAsync(IContainerMonitoringProcessor processor, CancellationToken cancellationToken)
        {
            if (_containerRegistry == null) 
                throw new ArgumentNullException(nameof(_containerRegistry));

            foreach (var container in _containerRegistry.GetContainers().Where(c => c.Info.Enabled))
            {
                container.LastIteration.DateTime = DateTime.Now;

                using var scope = Logger?.BeginScope(new LabelLogScope("scoped-container", container.Info.Name));

                try
                {
                    await processor.ProcessAsync(container, cancellationToken);
                }
                catch (Exception e)
                {
                    container.LastIteration.Error = e;
                    Logger?.Error(e).Write();
                }
            }
        }
    }
}
