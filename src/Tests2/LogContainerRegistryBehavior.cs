using MyLab.LogAgent;
using MyLab.LogAgent.Services;
using MyLab.LogAgent.Tools;

namespace Tests
{
    public class LogContainerRegistryBehavior
    {
        [Test]
        public void ShouldAddInitialContainers()
        {
            //Arrange
            var registry = new LogContainerRegistry();

            var initialContainers = new []
            {
                new DockerContainerInfo
                {
                    Id = "foo",
                    LogFormat = LogFormats.Default,
                    Name = "bar"
                }
            };

            //Act
            var syncReport = registry.Sync(initialContainers);

            //Assert
            Assert.NotNull(syncReport.Removed);
            Assert.IsEmpty(syncReport.Removed!);

            Assert.NotNull(syncReport.Added);
            Assert.NotNull(syncReport.Added!.SingleOrDefault(c => c.Id == "foo"));
        }

        [Test]
        public void ShouldAddNewAndRemoveAbsentContainers()
        {
            //Arrange
            var registry = new LogContainerRegistry();

            var initialContainers = new[]
            {
                new DockerContainerInfo
                {
                    Id = "foo",
                    LogFormat = LogFormats.Default,
                    Name = "foo-name"
                }
            };

            registry.Sync(initialContainers);

            var containers = new[]
            {
                new DockerContainerInfo
                {
                    Id = "bar",
                    LogFormat = LogFormats.Default,
                    Name = "bar-name"
                }
            };

            //Act
            var syncReport = registry.Sync(containers);

            //Assert
            Assert.NotNull(syncReport.Removed);
            Assert.NotNull(syncReport.Removed!.SingleOrDefault(c => c.Id == "foo"));

            Assert.NotNull(syncReport.Added);
            Assert.NotNull(syncReport.Added!.SingleOrDefault(c => c.Id == "bar"));

        }
    }
}