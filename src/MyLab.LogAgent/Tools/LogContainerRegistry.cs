using System.Collections;
using MyLab.Log;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Services;

namespace MyLab.LogAgent.Tools
{
    class LogContainerRegistry : IEnumerable<LogContainerRegistry.Entity>
    {
        readonly List<Entity> _entities = new();

        public SyncReport Sync(IEnumerable<DockerContainerInfo> containers)
        {
            if (containers == null) throw new ArgumentNullException(nameof(containers));

            var forRemove = _entities
                .Where(e => containers.All(c => c.Id != e.Container.Id))
                .Select(e => e.Container)
                .ToArray();
            var forAdd = containers
                .Where(c => _entities.All(e => e.Container.Id != c.Id))
                .ToArray();

            if(forAdd.Length == 0 && forRemove.Length == 0) return SyncReport.Empty;

            _entities.RemoveAll(e => forRemove.Any(r => r.Id == e.Container.Id));
            _entities.AddRange(forAdd.Select(c => new Entity { Container = c }));

            return new SyncReport
            {
                Added = forAdd,
                Removed = forRemove
            };
        }

        public IEnumerator<Entity> GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class Entity
        {
            public required DockerContainerInfo Container { get; init; }
            public long Shift { get; set; }
            public List<LogSourceLine> LineBuff { get; } = new();
            public string? LastLogFilename { get; set; }
            public ExceptionDto? LastError { get; set; }

            public DateTime? LastIterationDt { get; set; }
    }

        public class SyncReport
        {
            public static readonly SyncReport Empty = new ()
            {
                Added = Array.Empty<DockerContainerInfo>(),
                Removed = Array.Empty<DockerContainerInfo>()
            };

            public required IReadOnlyCollection<DockerContainerInfo> Removed { get; set; }
            public required IReadOnlyCollection<DockerContainerInfo> Added { get; set; }
        }
    }
}
