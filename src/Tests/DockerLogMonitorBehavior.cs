using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyLab.Log.XUnit;
using MyLab.LogAgent;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;
using MyLab.LogAgent.Services;
using Xunit.Abstractions;

namespace Tests
{
    public class DockerLogMonitorBehavior
    {
        private readonly ILogger<DockerLogMonitor> _logger;

        public DockerLogMonitorBehavior(ITestOutputHelper output)
        {
            _logger = new ServiceCollection()
                .AddLogging(b => b.AddFilter(_ => true).AddXUnit(output))
                .BuildServiceProvider()
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<DockerLogMonitor>();
        }

        [Fact]
        public async Task ShouldRegisterSimpleLogs()
        {
            //Arrange
            const string logLines = """
                           {"log":"Start log 1","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
                           {"log":"Start log 2","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
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
                                    {"log":"Start log 1","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
                                    {"log":" line 1","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
                                    {"log":" line 2","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
                                    {"log":"Start log 2","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
                                    {"log":" line 1","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
                                    {"log":" line 2","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
                                    """;

            var outgoingLogs = new List<LogRecord>();

            var monitor = CreateSingleLogCase(logLines, outgoingLogs);

            //Act
            await monitor.ProcessLogsAsync(default);

            //Assert
            Assert.Equal(2, outgoingLogs.Count);
            Assert.Equal("Start log 1", outgoingLogs[0].Message);
            Assert.NotNull(outgoingLogs[0].Properties);
            Assert.Contains(outgoingLogs[0].Properties!, p => p is 
            { 
                Name: LogPropertyNames.OriginMessage, 
                Value: """
                        Start log 1
                         line 1
                         line 2
                        """
            });
            Assert.Equal("Start log 2", outgoingLogs[1].Message);
            Assert.NotNull(outgoingLogs[1].Properties);
            Assert.Contains(outgoingLogs[1].Properties!, p => p is
            {
                Name: LogPropertyNames.OriginMessage,
                Value: """
                       Start log 2
                        line 1
                        line 2
                       """
            });
        }

        [Fact]
        public async Task RegisterNewLog()
        {
            //Arrange
            const string logLines = """
                                    {"log":"Start log 1","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
                                    {"log":"Start log 2","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
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
                    new ContainerFile("foo-id-json.log", 0)
                });
            filesProvider.Setup(p => p.OpenContainerFileRead
                (
                    It.Is<string>(id => id == "foo-id"),
                    It.Is<string>(fn => fn == "foo-id-json.log")
                ))
                .Returns<string, string>((_, _) =>
                {
                    var bytesStr = Encoding.UTF8.GetBytes(logContentString);
                    var memStream = new MemoryStream(bytesStr);
                    var reader = new StreamReader(memStream);
                    return reader;
                });

            var registrar = new Mock<ILogRegistrar>();
            registrar.Setup(r => r.RegisterAsync(It.IsAny<LogRecord>()))
                .Callback<LogRecord>(outgoingLogs.Add);

            var monitor = new DockerLogMonitor
            (
                containerProvider.Object,
                filesProvider.Object,
                registrar.Object,
                new OptionsWrapper<LogAgentOptions>(new LogAgentOptions()),
                _logger
            );

            await monitor.ProcessLogsAsync(default);
            logContentString = logLines + Environment.NewLine + "{\"log\":\"Start log 3\",\"stream\":\"stdout\",\"time\":\"2023-08-29T20:15:41.304555874Z\"}";

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
                                    {"log":"Start log 1","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
                                    {"log":"Start log 2","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
                                    """;

            const string log2Lines = "{\"log\":\"Start log 3\",\"stream\":\"stdout\",\"time\":\"2023-08-29T20:15:41.304555874Z\"}";

            var logFiles = new List<ContainerFile>
            {
                new ContainerFile("foo-id-json.log", 0)
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
                new OptionsWrapper<LogAgentOptions>(
                    new LogAgentOptions
                    {
                        ReadFromEnd = false
                    }),
                _logger
            );

            await monitor.ProcessLogsAsync(default);
            logFiles.Add(new ContainerFile("foo-id-json.log1", 0));

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
                    new ContainerFile("foo-id-json.log", 0)
                });
            filesProvider.Setup(p => p.EnumerateContainerFiles(It.Is<string>(id => id == "bar-id")))
                .Returns<string>(_ => new[]
                {
                    new ContainerFile("bar-id-json.log", 0)
                });
            filesProvider.Setup(p => p.OpenContainerFileRead
                (
                    It.Is<string>(id => id == "foo-id"),
                    It.Is<string>(fn => fn == "foo-id-json.log")
                ))
                .Returns<string, string>((_, _) => new StreamReader(new MemoryStream("{\"log\":\"ExtractCategory log\",\"stream\":\"stdout\",\"time\":\"2023-08-29T20:15:41.304555874Z\"}"u8.ToArray())));
            filesProvider.Setup(p => p.OpenContainerFileRead
                (
                    It.Is<string>(id => id == "bar-id"),
                    It.Is<string>(fn => fn == "bar-id-json.log")
                ))
                .Returns<string, string>((_, _) => new StreamReader(new MemoryStream("{\"log\":\"Bar log\",\"stream\":\"stdout\",\"time\":\"2023-08-29T20:15:41.304555874Z\"}"u8.ToArray())));

            var registrar = new Mock<ILogRegistrar>();
            registrar.Setup(r => r.RegisterAsync(It.IsAny<LogRecord>()))
                .Callback<LogRecord>(outgoingLogs.Add);

            var monitor = new DockerLogMonitor
            (
                containerProvider.Object,
                filesProvider.Object,
                registrar.Object,
                new OptionsWrapper<LogAgentOptions>(
                    new LogAgentOptions
                    {
                        ReadFromEnd = false
                    }),
                _logger
            );

            //Act
            await monitor.ProcessLogsAsync(default);

            //Assert
            Assert.Equal(2, outgoingLogs.Count);
            Assert.Contains(outgoingLogs, l => l.Message == "ExtractCategory log");
            Assert.Contains(outgoingLogs, l => l.Message == "Bar log");
        }

        private DockerLogMonitor CreateSingleLogCase(string logLines, List<LogRecord> outgoingLogs)
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
                    new ContainerFile("foo-id-json.log", 0)
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
                new OptionsWrapper<LogAgentOptions>(
                    new LogAgentOptions
                    {
                        ReadFromEnd = false
                    }),
                _logger
            );
            return monitor;
        }
    }
}
