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
            _log?.Debug("Monitoring started").Write();
            while (!stoppingToken.IsCancellationRequested)
            {
                _log?.Debug("New monitoring iteration").Write();

                try
                {
                    await _dockerLogMonitor.ProcessLogsAsync(stoppingToken);
                }
                catch (Exception e)
                {
                    _log?.Error(e).Write();
                }
                finally
                {
                    GC.Collect();
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            _log?.Debug("Monitoring stopped").Write();
        }
    }
}
