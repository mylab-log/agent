using System.Collections.Immutable;
using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.Services
{
    public interface IDockerContainerRegistry
    {
        DockerContainerSyncReport Sync(IEnumerable<DockerContainerInfo> containers);
        IEnumerable<DockerContainerMonitoringState> GetContainers();
    }

    class DockerContainerRegistry : IDockerContainerRegistry
    {
        readonly List<DockerContainerMonitoringState> _entities = new();

        public DockerContainerSyncReport Sync(IEnumerable<DockerContainerInfo> containers)
        {
            if (containers == null) throw new ArgumentNullException(nameof(containers));

            var forRemove = _entities
                .Where(e => containers.All(c => c.Id != e.Info.Id))
                .Select(e => e.Info)
                .ToArray();
            var forAdd = containers
                .Where(c => _entities.All(e => e.Info.Id != c.Id))
                .ToArray();

            if (forAdd.Length == 0 && forRemove.Length == 0) return DockerContainerSyncReport.Empty;

            _entities.RemoveAll(e => forRemove.Any(r => r.Id == e.Info.Id));
            _entities.AddRange(forAdd.Select(c => new DockerContainerMonitoringState { Info = c }));

            return new DockerContainerSyncReport
            {
                Added = forAdd,
                Removed = forRemove
            };
        }

        public IEnumerable<DockerContainerMonitoringState> GetContainers()
        {
            return _entities.ToImmutableArray();
        }
    }
}
