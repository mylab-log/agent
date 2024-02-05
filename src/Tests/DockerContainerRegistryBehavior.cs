using MyLab.LogAgent;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Services;
using MyLab.LogAgent.Tools;

namespace Tests
{
    public class DockerContainerRegistryBehavior
    {
        [Fact]
        public void ShouldAddInitialContainers()
        {
            //Arrange
            var registry = new DockerContainerRegistry();

            var initialContainers = new []
            {
                new DockerContainerInfo
                {
                    Id = "foo",
                    Name = "bar"
                }
            };

            //Act
            var syncReport = registry.Sync(initialContainers);

            //Assert
            Assert.NotNull(syncReport.Removed);
            Assert.Empty(syncReport.Removed!);

            Assert.NotNull(syncReport.Added);
            Assert.Contains(syncReport.Added, c => c.Info.Id == "foo");
        }

        [Fact]
        public void ShouldAddNewAndRemoveAbsentContainers()
        {
            //Arrange
            var registry = new DockerContainerRegistry();

            var initialContainers = new[]
            {
                new DockerContainerInfo
                {
                    Id = "foo",
                    Name = "foo-name"
                }
            };

            registry.Sync(initialContainers);

            var containers = new[]
            {
                new DockerContainerInfo
                {
                    Id = "bar",
                    Name = "bar-name"
                }
            };

            //Act
            var syncReport = registry.Sync(containers);

            //Assert
            Assert.NotNull(syncReport.Removed);
            Assert.Contains(syncReport.Removed, c => c.Info.Id == "foo");

            Assert.NotNull(syncReport.Added);
            Assert.Contains(syncReport.Added, c => c.Info.Id == "bar");

        }
    }
}