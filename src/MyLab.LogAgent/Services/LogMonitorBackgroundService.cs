using MyLab.Log.Dsl;

namespace MyLab.LogAgent.Services
{
    class LogMonitorBackgroundService(IDockerLogMonitor dockerLogMonitor, ILogger<LogMonitorBackgroundService>? logger = null) : BackgroundService
    {
        private readonly IDockerLogMonitor _dockerLogMonitor = dockerLogMonitor 
                                        ?? throw new ArgumentNullException(nameof(dockerLogMonitor));

        private readonly IDslLogger? _log = logger?.Dsl();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
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
