using System.Collections.Immutable;
using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.Services
{
    public interface IDockerContainerRegistry
    {
        DockerContainerSyncReport Sync(IEnumerable<DockerContainerInfo> containers);
        ImmutableArray<DockerContainerMonitoringState> GetContainers();
    }

    class DockerContainerRegistry : IDockerContainerRegistry
    {
        readonly List<DockerContainerMonitoringState> _entities = new();

        public DockerContainerSyncReport Sync(IEnumerable<DockerContainerInfo> containers)
        {
            if (containers == null) throw new ArgumentNullException(nameof(containers));

            var forRemove = _entities
                .Where(e => containers.All(c => c.Id != e.Container.Id))
                .Select(e => e.Container)
                .ToArray();
            var forAdd = containers
                .Where(c => _entities.All(e => e.Container.Id != c.Id))
                .ToArray();

            if (forAdd.Length == 0 && forRemove.Length == 0) return DockerContainerSyncReport.Empty;

            _entities.RemoveAll(e => forRemove.Any(r => r.Id == e.Container.Id));
            _entities.AddRange(forAdd.Select(c => new DockerContainerMonitoringState { Container = c }));

            return new DockerContainerSyncReport
            {
                Added = forAdd,
                Removed = forRemove
            };
        }

        public ImmutableArray<DockerContainerMonitoringState> GetContainers()
        {
            return _entities.ToImmutableArray();
        }
    }
}
