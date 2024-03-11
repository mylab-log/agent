using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using MyLab.Log.XUnit;
using MyLab.LogAgent;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Services;
using MyLab.Search.EsAdapter;
using MyLab.Search.EsAdapter.Search;
using MyLab.Search.EsTest;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class LogAgentBehavior : IClassFixture<EsFixture<TestEsFixtureStrategy>>, IAsyncLifetime
    {
        private readonly EsFixture<TestEsFixtureStrategy> _fxt;
        private readonly ITestOutputHelper _output;

        public LogAgentBehavior(EsFixture<TestEsFixtureStrategy> fxt, ITestOutputHelper output)
        {
            fxt.Output = output;
            _fxt = fxt;
            _output = output;
        }

        [Fact]
        public async Task ShouldDetectException()
        {
            //Arrange
            var testContainer = new DockerContainerInfo
            {
                Id = "lost-exception",
                Name = "lost-exception",
                LogFormat = "mylab"
            };

            var monitorService = CreateApp(testContainer, omitLblNs: true);

            //Act
            var startToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await monitorService.StartAsync(startToken.Token);

            await Task.Delay(TimeSpan.FromSeconds(1), default(CancellationToken));

            var stopToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await monitorService.StopAsync(stopToken.Token);

            await Task.Delay(TimeSpan.FromSeconds(1), default(CancellationToken));

            var searchRes = await _fxt.Searcher.SearchAsync("logs-test",
                new EsSearchParams<EsLogRecord>(d => d.MatchAll()),
                default
            );

            var found = searchRes.FirstOrDefault();

            //Assert
            Assert.Single(searchRes);
            Assert.NotNull(found);
            Assert.Contains(found, kv => kv is { Key: LogPropertyNames.Exception });
        }

        [Fact]
        public async Task ShouldIndexMultilineDockerLogs()
        {
            //Arrange
            var testContainer = new DockerContainerInfo
            {
                Id = "multiline",
                Name = "multiline",
                Labels = new Dictionary<DockerLabelName, string>
                {
                    {"ns1.ns2.foo", "bar"}
                }
            };

            var monitorService = CreateApp(testContainer, omitLblNs: true);

            //Act
            var startToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await monitorService.StartAsync(startToken.Token);

            await Task.Delay(TimeSpan.FromSeconds(1), default(CancellationToken));

            var stopToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await monitorService.StopAsync(stopToken.Token);

            await Task.Delay(TimeSpan.FromSeconds(1), default(CancellationToken));

            var searchRes = await _fxt.Searcher.SearchAsync("logs-test", 
                new EsSearchParams<EsLogRecord>(d => d.MatchAll()), 
                default
                );

            //Assert
            Assert.Equal(4, searchRes.Count);
            Assert.Contains(searchRes, rec =>
                rec.Any(p => p is { Key: LogPropertyNames.Message, Value: "InstrumentationLibrarySpans #0" }) &&
                rec.Any(p => p is { Key: LogPropertyNames.Time, Value: "2023-08-29T20:15:41.3045559Z" })
            );
            Assert.Contains(searchRes, rec =>
                rec.Any(p => p is { Key: LogPropertyNames.Message, Value: "InstrumentationLibrary OpenTelemetry.Instrumentation.AspNetCore 1.0.0.0" }) && 
                rec.Any(p => p is { Key: LogPropertyNames.Time, Value: "2023-08-29T20:15:41.3045745Z" })
            );
            Assert.Contains(searchRes, rec =>
                rec.Any(p => p.Key == LogPropertyNames.Message && ((string)p.Value).Contains("Span #0")) && 
                rec.Any(p => p is { Key: LogPropertyNames.Time, Value: "2023-08-29T20:15:41.3045815Z" }) &&
                rec.Any(p => p.Key == LogPropertyNames.OriginMessage && ((string)p.Value).Contains("Trace ID       : f15bcb09a61119c219067946020cd5a1"))
            );
        }

        [Fact]
        public async Task ShouldIndexSqlDockerLogs()
        {
            //Arrange
            var testContainer = new DockerContainerInfo
            {
                Id = "mylab-with-sql",
                Name = "mylab-with-sql",
                LogFormat = "mylab"
            };

            var monitorService = CreateApp(testContainer);

            //Act
            var startToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await monitorService.StartAsync(startToken.Token);

            await Task.Delay(TimeSpan.FromSeconds(1), default(CancellationToken));

            var stopToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await monitorService.StopAsync(stopToken.Token);

            await Task.Delay(TimeSpan.FromSeconds(1), default(CancellationToken));

            var searchRes = await _fxt.Searcher.SearchAsync("logs-test",
                new EsSearchParams<EsLogRecord>(d => d.MatchAll()),
                default
                );

            //Assert
            Assert.Equal(2, searchRes.Count);
            Assert.Contains(searchRes, rec =>
                rec.Any(p => p is { Key: LogPropertyNames.Message, Value: "DB query" }) &&
                rec.Any(p => p is { Key: "trace-id", Value: "c300bef21768286157116d1feed3f1d2" }) &&
                rec.Any(p => p.Key == "SqlText" && ((string)p.Value).Contains("`t`.`ext_system_id`"))
            );
            Assert.Contains(searchRes, rec =>
                rec.Any(p => p is { Key: LogPropertyNames.Message, Value: "DB query" }) &&
                rec.Any(p => p is { Key: "trace-id", Value: "c300bef21768286157116d1feed3f1d2" }) && 
                rec.Any(p => p.Key == "SqlText" && ((string)p.Value).Contains("WHEN EXISTS("))
            );
        }

        [Fact]
        public async Task ShouldIndexDefaultFormatLogs()
        {
            //Arrange
            var testContainer = new DockerContainerInfo
            {
                Id = "strange-err",
                Name = "strange-err",
                Labels = new Dictionary<DockerLabelName, string>
                {
                    {"ns1.ns2.foo", "bar"}
                }
            };

            var monitorService = CreateApp(testContainer);

            //Act
            var startToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await monitorService.StartAsync(startToken.Token);

            await Task.Delay(TimeSpan.FromSeconds(1), default(CancellationToken));

            var stopToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await monitorService.StopAsync(stopToken.Token);
            
            await Task.Delay(TimeSpan.FromSeconds(3), default(CancellationToken));

            var searchRes = await _fxt.Searcher.SearchAsync("logs-test",
                new EsSearchParams<EsLogRecord>(d => d.MatchAll())
                {
                    Paging = new EsPaging
                    {
                        From = 0,
                        Size = 500
                    }
                },
                default
            );

            //Assert
            Assert.Equal(340, searchRes.Count);
            Assert.Contains(searchRes, rec =>
                rec.Any(p => p is { Key: LogPropertyNames.Message, Value: "Установка сертифкатов с приватным ключом." }) &&
                rec.All(p => p is not { Key: LogPropertyNames.Level, Value: "error" })
            );
        }

        private IHostedService CreateApp(DockerContainerInfo testContainer, bool omitLblNs = false)
        {
            var containerProvider = new Mock<IDockerContainerProvider>();
            containerProvider
                .Setup(p => p.ProvideContainersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Enumerable.Repeat(testContainer, 1));

            var srv = new ServiceCollection()
                .AddLogAgentLogic()
                .AddEsTools()
                .AddSingleton(containerProvider.Object)
                .ConfigureLogAgentLogic(opt =>
                {
                    opt.Docker.ContainersPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                    opt.OutgoingBufferSize = 500;
                    opt.ReadFromEnd = false;
                    opt.Docker.OmitLabelNamespace = omitLblNs;
                })
                .ConfigureEsTools(opt =>
                {
                    opt.Url = TestStuff.EsUrl;
                    opt.IndexBindings = new IndexBinding[]
                    {
                        new() { Doc = "log", Index = "logs-test" }
                    };
                })
                .AddLogging(b => b.AddFilter(_ => true).AddXUnit(_output))
                .BuildServiceProvider();

            var monitorService = srv.GetRequiredService<IHostedService>();
            if (monitorService is not LogMonitorBackgroundService)
                throw new InvalidOperationException($"Wrong hosted service type: {monitorService.GetType().FullName}");
            return monitorService;
        }

        public Task InitializeAsync()
        {
            return _fxt.Tools.Index("logs-test").PruneAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}