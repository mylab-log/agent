using MyLab.LogAgent.Options;
using MyLab.LogAgent.Services;
using MyLab.Search.EsAdapter;

namespace MyLab.LogAgent
{
    static class LogAgentLogic
    {
        public static IServiceCollection AddLogAgentLogic(this IServiceCollection services)
        {
            return services
                .AddHostedService<LogMonitorBackgroundService>()
                .AddSingleton<IDockerContainerProvider, DockerContainerProvider>()
                .AddSingleton<IDockerLogMonitor, DockerLogMonitor>()
                .AddSingleton<IDockerContainerFilesProvider, DockerContainerFilesProvider>()
                .AddSingleton<ILogRegistrar, LogRegistrar>()
                .AddSingleton<ILogRegistrationTransport, LogRegistrationTransport>();
        }

        public static IServiceCollection ConfigureLogAgentLogic(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .Configure<LogAgentOptions>(configuration.GetSection("LogAgent"));
        }

        public static IServiceCollection ConfigureLogAgentLogic(this IServiceCollection services, Action<LogAgentOptions> configurator)
        {
            return services
                .Configure(configurator);
        }
    }
}
