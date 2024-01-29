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
using Nest;
using Newtonsoft.Json.Linq;
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
        public async Task ShouldIndexMultilineDockerLogs()
        {
            //Arrange
            var testContainer = new DockerContainerInfo
            {
                Id = "multiline",
                Name = "multiline"
            };

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
                    opt.DockerContainersPath = Path.Combine( Directory.GetCurrentDirectory(), "logs");
                    opt.OutgoingBufferSize = 1;
                    opt.ReadFromEnd = false;
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
                rec.Any(p => p.Key == LogPropertyNames.Message && p.Value.Contains("Span #0")) && 
                rec.Any(p => p is { Key: LogPropertyNames.Time, Value: "2023-08-29T20:15:41.3045815Z" }) &&
                rec.Any(p => p.Key == LogPropertyNames.OriginMessage && p.Value.Contains("Trace ID       : f15bcb09a61119c219067946020cd5a1"))
            );
        }

        //[Fact]
        //public async Task ShouldIndexBad()
        //{
        //    //Arrange
        //    var testContainer = new DockerContainerInfo
        //    {
        //        Id = "bad",
        //        Name = "bad",
        //        LogFormat = "mylab"
        //    };

        //    var containerProvider = new Mock<IDockerContainerProvider>();
        //    containerProvider
        //        .Setup(p => p.ProvideContainersAsync(It.IsAny<CancellationToken>()))
        //        .ReturnsAsync(() => Enumerable.Repeat(testContainer, 1));

        //    var srv = new ServiceCollection()
        //        .AddLogAgentLogic()
        //        .AddEsTools()
        //        .AddSingleton(containerProvider.Object)
        //        .ConfigureLogAgentLogic(opt =>
        //        {
        //            opt.DockerContainersPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        //            opt.OutgoingBufferSize = 1;
        //            opt.ReadFromEnd = false;
        //        })
        //        .ConfigureEsTools(opt =>
        //        {
        //            opt.Url = TestStuff.EsUrl;
        //            opt.IndexBindings = new IndexBinding[]
        //            {
        //                new() { Doc = "log", Index = "logs-test" }
        //            };
        //        })
        //        .AddLogging(b => b.AddFilter(_ => true).AddXUnit(_output))
        //        .BuildServiceProvider();

        //    var monitorService = srv.GetRequiredService<IHostedService>();
        //    if (monitorService is not LogMonitorBackgroundService)
        //        throw new InvalidOperationException($"Wrong hosted service type: {monitorService.GetType().FullName}");

        //    //Act
        //    var startToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        //    await monitorService.StartAsync(default);

        //    await Task.Delay(TimeSpan.FromSeconds(5), default(CancellationToken));
        //    //await Task.Delay(TimeSpan.FromMinutes(1), default(CancellationToken));

        //    var stopToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        //    await monitorService.StopAsync(default);

        //    var searchRes = await _fxt.Searcher.SearchAsync("logs-test",
        //        new EsSearchParams<EsLogRecord>(d => d.MatchAll()),
        //        default
        //        );

        //    //Assert
        //}

        public Task InitializeAsync()
        {
            return _fxt.IndexTools.PruneIndexAsync("logs-test");
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}