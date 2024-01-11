using System.Text;
using Moq;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Services;

namespace Tests
{
    public class DockerLogMonitorBehavior
    {
        [Fact]
        public async Task ShouldRegisterSimpleLogs()
        {
            //Arrange
            const string logLines = """
                           Start log 1
                           Start log 2
                           """;

            var outgoingLogs = new List<LogRecord>();

            var monitor = CreateSingleLogCase(logLines, outgoingLogs);

            //Act
            await monitor.ProcessLogsAsync(default);

            //Assert
            Assert.Equal(2, outgoingLogs.Count);
            Assert.Equal("Start log 1", outgoingLogs[0].Message);
            Assert.Equal("Start log 2", outgoingLogs[1].Message);
        }

        [Fact]
        public async Task ShouldRegisterMultilineLogs()
        {
            //Arrange
            const string logLines = """
                                    Start log 1
                                     line 1
                                     line 2
                                    Start log 2
                                     line 1
                                     line 2
                                    """;

            var outgoingLogs = new List<LogRecord>();

            var monitor = CreateSingleLogCase(logLines, outgoingLogs);

            //Act
            await monitor.ProcessLogsAsync(default);

            //Assert
            Assert.Equal(2, outgoingLogs.Count);
            Assert.Equal("""
                         Start log 1
                          line 1
                          line 2
                         """, outgoingLogs[0].Message);
            Assert.Equal("""
                         Start log 2
                          line 1
                          line 2
                         """, outgoingLogs[1].Message);
        }

        [Fact]
        public async Task ShouldRegisterMultilineLogsWithEmptyStrings()
        {
            //Arrange
            const string logLines = """
                                    Start log 1
                                     line 1
                                     line 2
                                     
                                     
                                    Start log 2
                                     line 1
                                     line 2
                                    """;

            var outgoingLogs = new List<LogRecord>();

            var monitor = CreateSingleLogCase(logLines, outgoingLogs);

            //Act
            await monitor.ProcessLogsAsync(default);

            //Assert
            Assert.Equal(2, outgoingLogs.Count);
            Assert.Equal("""
                         Start log 1
                          line 1
                          line 2
                         """, outgoingLogs[0].Message);
            Assert.Equal("""
                         Start log 2
                          line 1
                          line 2
                         """, outgoingLogs[1].Message);
        }

        [Fact]
        public async Task RegisterNewLog()
        {
            //Arrange
            const string logLines = """
                                    Start log 1
                                    Start log 2
                                    """;
            string logContentString = logLines;

            var fooContainer = new DockerContainerInfo
            {
                Id = "foo-id", Name = "foo-container" 
            };

            var outgoingLogs = new List<LogRecord>();

            var containerProvider = new Mock<IDockerContainerProvider>();
            containerProvider
                .Setup(p => p.ProvideContainersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationToken _) => new [] { fooContainer }
                );

            var filesProvider = new Mock<IDockerContainerFilesProvider>();
            filesProvider.Setup(p => p.EnumerateContainerFiles(It.Is<string>(id => id == "foo-id")))
                .Returns<string>(_ => new[]
                {
                    "foo-id-json.log"
                });
            filesProvider.Setup(p => p.OpenContainerFileRead
                (
                    It.Is<string>(id => id == "foo-id"),
                    It.Is<string>(fn => fn == "foo-id-json.log")
                ))
                .Returns<string, string>((_, _) => new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(logContentString))));

            var registrar = new Mock<ILogRegistrar>();
            registrar.Setup(r => r.RegisterAsync(It.IsAny<LogRecord>()))
                .Callback<LogRecord>(outgoingLogs.Add);

            var monitor = new DockerLogMonitor
            (
                containerProvider.Object,
                filesProvider.Object,
                registrar.Object,
                new EmptyContextPropertiesProvider()
            );

            await monitor.ProcessLogsAsync(default);
            logContentString = logLines + Environment.NewLine + "Start log 3";

            //Act
            await monitor.ProcessLogsAsync(default);

            //Assert
            Assert.Equal(3, outgoingLogs.Count);
            Assert.Equal("Start log 1", outgoingLogs[0].Message);
            Assert.Equal("Start log 2", outgoingLogs[1].Message);
            Assert.Equal("Start log 3", outgoingLogs[2].Message);
        }

        [Fact]
        public async Task ShouldRegisterFromNewFile()
        {
            //Arrange
            const string log1Lines = """
                                    Start log 1
                                    Start log 2
                                    """;

            const string log2Lines = "Start log 3";

            var logFiles = new List<string>
            {
                "foo-id-json.log"
            };

            var outgoingLogs = new List<LogRecord>();

            var containerProvider = new Mock<IDockerContainerProvider>();
            containerProvider
                .Setup(p => p.ProvideContainersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationToken _) => new DockerContainerInfo[]
                {
                    new() { Id = "foo-id", Name = "foo-container"}
                });

            var filesProvider = new Mock<IDockerContainerFilesProvider>();
            filesProvider.Setup(p => p.EnumerateContainerFiles(It.Is<string>(id => id == "foo-id")))
                .Returns<string>(_ => logFiles);
            filesProvider.Setup(p => p.OpenContainerFileRead
                (
                    It.Is<string>(id => id == "foo-id"),
                    It.Is<string>(fn => fn == "foo-id-json.log")
                ))
                .Returns<string, string>((_, _) => new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(log1Lines))));
            filesProvider.Setup(p => p.OpenContainerFileRead
                (
                    It.Is<string>(id => id == "foo-id"),
                    It.Is<string>(fn => fn == "foo-id-json.log1")
                ))
                .Returns<string, string>((_, _) => new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(log2Lines))));

            var registrar = new Mock<ILogRegistrar>();
            registrar.Setup(r => r.RegisterAsync(It.IsAny<LogRecord>()))
                .Callback<LogRecord>(outgoingLogs.Add);

            var monitor = new DockerLogMonitor
            (
                containerProvider.Object,
                filesProvider.Object,
                registrar.Object,
                new EmptyContextPropertiesProvider()
            );

            await monitor.ProcessLogsAsync(default);
            logFiles.Add("foo-id-json.log1");

            //Act
            await monitor.ProcessLogsAsync(default);

            //Assert
            Assert.Equal(3, outgoingLogs.Count);
            Assert.Equal("Start log 1", outgoingLogs[0].Message);
            Assert.Equal("Start log 2", outgoingLogs[1].Message);
            Assert.Equal("Start log 3", outgoingLogs[2].Message);
        }

        [Fact]
        public async Task ShouldRegisterLogsFromSeveralContainers()
        {
            //Arrange
            var outgoingLogs = new List<LogRecord>();

            var containerProvider = new Mock<IDockerContainerProvider>();
            containerProvider
                .Setup(p => p.ProvideContainersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationToken ct) => new DockerContainerInfo[]
                {
                    new() { Id = "foo-id", Name = "foo-container"},
                    new() { Id = "bar-id", Name = "bar-container"}
                });

            var filesProvider = new Mock<IDockerContainerFilesProvider>();
            filesProvider.Setup(p => p.EnumerateContainerFiles(It.Is<string>(id => id == "foo-id")))
                .Returns<string>(_ => new[]
                {
                    "foo-id-json.log"
                });
            filesProvider.Setup(p => p.EnumerateContainerFiles(It.Is<string>(id => id == "bar-id")))
                .Returns<string>(_ => new[]
                {
                    "bar-id-json.log"
                });
            filesProvider.Setup(p => p.OpenContainerFileRead
                (
                    It.Is<string>(id => id == "foo-id"),
                    It.Is<string>(fn => fn == "foo-id-json.log")
                ))
                .Returns<string, string>((_, _) => new StreamReader(new MemoryStream("Foo log"u8.ToArray())));
            filesProvider.Setup(p => p.OpenContainerFileRead
                (
                    It.Is<string>(id => id == "bar-id"),
                    It.Is<string>(fn => fn == "bar-id-json.log")
                ))
                .Returns<string, string>((_, _) => new StreamReader(new MemoryStream("Bar log"u8.ToArray())));

            var registrar = new Mock<ILogRegistrar>();
            registrar.Setup(r => r.RegisterAsync(It.IsAny<LogRecord>()))
                .Callback<LogRecord>(outgoingLogs.Add);

            var monitor = new DockerLogMonitor
            (
                containerProvider.Object,
                filesProvider.Object,
                registrar.Object,
                new EmptyContextPropertiesProvider()
            );

            //Act
            await monitor.ProcessLogsAsync(default);

            //Assert
            Assert.Equal(2, outgoingLogs.Count);
            Assert.Contains(outgoingLogs, l => l.Message == "Foo log");
            Assert.Contains(outgoingLogs, l => l.Message == "Bar log");
        }

        private static DockerLogMonitor CreateSingleLogCase(string logLines, List<LogRecord> outgoingLogs)
        {
            var containerProvider = new Mock<IDockerContainerProvider>();
            containerProvider
                .Setup(p => p.ProvideContainersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationToken ct) => new DockerContainerInfo[]
                {
                    new() { Id = "foo-id", Name = "foo-container"}
                });

            var filesProvider = new Mock<IDockerContainerFilesProvider>();
            filesProvider.Setup(p => p.EnumerateContainerFiles(It.Is<string>(id => id == "foo-id")))
                .Returns<string>(_ => new []
                {
                    "foo-id-json.log"
                });
            filesProvider.Setup(p => p.OpenContainerFileRead
                (
                    It.Is<string>(id => id == "foo-id"),
                    It.Is<string>(fn => fn == "foo-id-json.log")
                ))
                .Returns<string, string>((_, _) => new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(logLines))));

            var registrar = new Mock<ILogRegistrar>();
            registrar.Setup(r => r.RegisterAsync(It.IsAny<LogRecord>()))
                .Callback<LogRecord>(outgoingLogs.Add);

            var monitor = new DockerLogMonitor
            (
                containerProvider.Object,
                filesProvider.Object,
                registrar.Object,
                new EmptyContextPropertiesProvider()
            );
            return monitor;
        }

        class EmptyContextPropertiesProvider : IContextPropertiesProvider
        {
            public IEnumerable<LogProperty> ProvideProperties()
            {
                return Enumerable.Empty<LogProperty>();
            }
        }
    }
}
