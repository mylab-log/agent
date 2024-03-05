using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyLab.Log.Dsl;
using MyLab.Log.XUnit;
using MyLab.LogAgent;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;
using MyLab.LogAgent.Services;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Tests
{
    public class DockerLogMonitorBehavior
    {
        private readonly ILogger<DockerLogMonitor> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public DockerLogMonitorBehavior(ITestOutputHelper output)
        {
            _loggerFactory = new ServiceCollection()
                .AddLogging(b => b.AddFilter(_ => true).AddXUnit(output))
                .BuildServiceProvider()
                .GetRequiredService<ILoggerFactory>();
            _logger = _loggerFactory.CreateLogger<DockerLogMonitor>();
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

            var containerProvider = CreateDockerContainerProvider(
                new DockerContainerInfo
                {
                    Id = "foo-id", 
                    Name = "foo-container",
                    LogFormat = "single"
                }
            );

            var filesProvider = ProvideContainerFileProvider(
                "foo-id",
                new ContainerFile("foo-id-json.log", 0),
                () => logLines);

            var registrar = CreateLogRegistrar(outgoingLogs);

            var cMonitoringProcessor = new ContainerMonitoringProcessor(
                filesProvider.Object,
                registrar,
                null,
                new OptionsWrapper<LogAgentOptions>(new LogAgentOptions { ReadFromEnd = false }),
                _loggerFactory.CreateLogger<ContainerMonitoringProcessor>());

            var monitor = new DockerLogMonitor
            (
                containerProvider,
                new DockerContainerRegistry(),
                cMonitoringProcessor,
                null,
                _logger
            );

            //Act
            await monitor.ProcessLogsAsync(default);

            //Assert
            Assert.Equal(2, outgoingLogs.Count);
            Assert.Equal("Start log 1", outgoingLogs[0].Message);
            Assert.Equal("Start log 2", outgoingLogs[1].Message);
        }

        [Fact]
        public async Task ShouldAddContainerLabels()
        {
            //Arrange
            const string logLines = """
                                    {"log":"Message","stream":"stdout","time":"2023-08-29T20:15:41.304555874Z"}
                                    """;

            var outgoingLogs = new List<LogRecord>();

            var containerProvider = CreateDockerContainerProvider(
                new DockerContainerInfo
                {
                    Id = "foo-id",
                    Name = "foo-container",
                    LogFormat = "single",
                    Labels = new Dictionary<string, string>
                    {
                        { "foo", "bar" },
                        { "baz", "qoz" }
                    }
                }
            );

            var filesProvider = ProvideContainerFileProvider(
                "foo-id",
                new ContainerFile("foo-id-json.log", 0),
                () => logLines);

            var registrar = CreateLogRegistrar(outgoingLogs);

            var cMonitoringProcessor = new ContainerMonitoringProcessor(
                filesProvider.Object,
                registrar,
                null,
                new OptionsWrapper<LogAgentOptions>(new LogAgentOptions { ReadFromEnd = false }),
                _loggerFactory.CreateLogger<ContainerMonitoringProcessor>());

            var monitor = new DockerLogMonitor
            (
                containerProvider,
                new DockerContainerRegistry(),
                cMonitoringProcessor,
                null,
                _logger
            );

            //Act
            await monitor.ProcessLogsAsync(default);

            var labels = outgoingLogs
                .FirstOrDefault()?
                .Properties?
                .ToDictionary()
                .Where(p => p.Key == LogPropertyNames.ContainerLabels)
                .Select(p => (IDictionary<string, string>)p.Value)
                .FirstOrDefault();

            //Assert
            Assert.NotNull(labels);
            Assert.Contains(labels, kv => kv is { Key: "foo", Value: "bar" });
            Assert.Contains(labels, kv => kv is { Key: "baz", Value: "qoz" });
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

            var containerProvider = CreateDockerContainerProvider(
                new DockerContainerInfo
                {
                    Id = "foo-id", 
                    Name = "foo-container"
                }
            );

            var filesProvider = ProvideContainerFileProvider(
                "foo-id",
                new ContainerFile("foo-id-json.log", 0),
                () => logLines);

            var registrar = CreateLogRegistrar(outgoingLogs);

            var cMonitoringProcessor = new ContainerMonitoringProcessor(
                filesProvider.Object,
                registrar,
                null,
                new OptionsWrapper<LogAgentOptions>(new LogAgentOptions { ReadFromEnd = false }),
                _loggerFactory.CreateLogger<ContainerMonitoringProcessor>());

            var monitor = new DockerLogMonitor
            (
                containerProvider,
                new DockerContainerRegistry(),
                cMonitoringProcessor,
                null,
                _logger
            );

            //Act
            await monitor.ProcessLogsAsync(default);

            //Assert
            Assert.Equal(2, outgoingLogs.Count);
            Assert.Equal("Start log 1", outgoingLogs[0].Message);
            Assert.NotNull(outgoingLogs[0].Properties);
            Assert.Contains(outgoingLogs[0].Properties!, p => p is 
            { 
                Key: LogPropertyNames.OriginMessage, 
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
                Key: LogPropertyNames.OriginMessage,
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

            var outgoingLogs = new List<LogRecord>();
            var registrar = CreateLogRegistrar(outgoingLogs);

            var containerProvider = CreateDockerContainerProvider(
                new DockerContainerInfo
                {
                    Id = "foo-id",
                    Name = "foo-container",
                    LogFormat = "single"
                });

            var filesProvider = ProvideContainerFileProvider(
                "foo-id", 
                new ContainerFile("foo-id-json.log", 0),
                () => logContentString);
            
            var cMonitoringProcessor = new ContainerMonitoringProcessor(
                filesProvider.Object, 
                registrar, 
                null,
                new OptionsWrapper<LogAgentOptions>(new LogAgentOptions()), 
                _loggerFactory.CreateLogger<ContainerMonitoringProcessor>());

            var monitor = new DockerLogMonitor
            (
                containerProvider,
                new DockerContainerRegistry(),
                cMonitoringProcessor,
                null,
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
        public async Task ShouldRegisterLogsFromSeveralContainers()
        {
            //Arrange
            var outgoingLogs = new List<LogRecord>();
            var registrar = CreateLogRegistrar(outgoingLogs);

            var containerProvider = CreateDockerContainerProvider(
                new() { Id = "foo-id", Name = "foo-container" },
                new() { Id = "bar-id", Name = "bar-container" }
            );

            var filesProvider = ProvideContainerFileProvider(
                "foo-id",
                new ContainerFile("foo-id-json.log", 0),
                () => "{\"log\":\"ExtractCategory log\",\"stream\":\"stdout\",\"time\":\"2023-08-29T20:15:41.304555874Z\"}"
            );
            filesProvider = ProvideContainerFileProvider(
                "bar-id",
                new ContainerFile("bar-id-json.log", 0),
                () => "{\"log\":\"Bar log\",\"stream\":\"stdout\",\"time\":\"2023-08-29T20:15:41.304555874Z\"}",
                filesProvider
            );

            var cMonitoringProcessor = new ContainerMonitoringProcessor(
                filesProvider.Object,
                registrar,
                null,
                new OptionsWrapper<LogAgentOptions>(new LogAgentOptions{ ReadFromEnd = false}),
                _loggerFactory.CreateLogger<ContainerMonitoringProcessor>());

            var monitor = new DockerLogMonitor
            (
                containerProvider,
                new DockerContainerRegistry(),
                cMonitoringProcessor,
                null,
                _logger
            );

            //Act
            await monitor.ProcessLogsAsync(default);

            //Assert
            Assert.Equal(2, outgoingLogs.Count);
            Assert.Contains(outgoingLogs, l => l.Message == "ExtractCategory log");
            Assert.Contains(outgoingLogs, l => l.Message == "Bar log");
        }

        IDockerContainerProvider CreateDockerContainerProvider(params DockerContainerInfo[] containers)
        {
            var containerProvider = new Mock<IDockerContainerProvider>();
            containerProvider
                .Setup(p => p.ProvideContainersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationToken ct) => containers);

            return containerProvider.Object;
        }

        Mock<IDockerContainerFilesProvider> ProvideContainerFileProvider(string fileId, ContainerFile file, Func<string> contentProvider, Mock<IDockerContainerFilesProvider>? existentProvider = null)
        {
            var filesProvider = existentProvider ?? new Mock<IDockerContainerFilesProvider>();
            filesProvider.Setup(p => p.GetActualContainerLogFile(It.Is<string>(id => id == fileId)))
                .Returns<string>(_ => file);
            filesProvider.Setup(p => p.OpenContainerFileRead
                (
                    It.Is<string>(id => id == fileId),
                    It.Is<string>(fn => fn == file.Filename)
                ))
                .Returns<string, string>((_, _) =>
                {
                    var bytesStr = Encoding.UTF8.GetBytes(contentProvider());
                    var memStream = new MemoryStream(bytesStr);
                    var reader = new StreamReader(memStream);
                    return reader;
                });
            return filesProvider;
        }

        ILogRegistrar CreateLogRegistrar(List<LogRecord> outgoingLogs)
        {
            var registrar = new Mock<ILogRegistrar>();
            registrar.Setup(r => r.RegisterAsync(It.IsAny<LogRecord>(), It.IsAny<CancellationToken>()))
                .Callback<LogRecord, CancellationToken>((lr, _) => outgoingLogs.Add(lr));

            return registrar.Object;
        }
    }
}
