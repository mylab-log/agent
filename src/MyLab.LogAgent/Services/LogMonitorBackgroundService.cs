using System.Runtime.InteropServices.ComTypes;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.LogAgent.Options;

namespace MyLab.LogAgent.Services
{
    class LogMonitorBackgroundService : BackgroundService
    {
        private readonly IDockerLogMonitor _dockerLogMonitor;

        private readonly IDslLogger? _log;
        private readonly LogAgentOptions _opts;

        public LogMonitorBackgroundService(
            IDockerLogMonitor dockerLogMonitor, 
            IOptions<LogAgentOptions> opts,
            ILogger<LogMonitorBackgroundService>? logger = null)
        {
            _dockerLogMonitor = dockerLogMonitor 
                                     ?? throw new ArgumentNullException(nameof(dockerLogMonitor));
            _log = logger?.Dsl();
            _opts = opts.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log?.Action("Log processing started")
                .AndFactIs("config", _opts)
                .Write();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _dockerLogMonitor.ProcessLogsAsync(stoppingToken);
                }
                catch (Exception e)
                {
                    _log?.Error(e).Write();
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
