using MyLab.LogAgent.Options;
using MyLab.LogAgent.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddHostedService<LogMonitorBackgroundService>()
    .AddSingleton<IDockerContainerProvider, DockerContainerProvider>()
    .AddSingleton<IDockerLogMonitor, DockerLogMonitor>()
    .AddSingleton<IDockerContainerFilesProvider, DockerContainerFilesProvider>()
    .AddSingleton<ILogRegistrar, LogRegistrar>()
    .AddSingleton<ILogRegistrationTransport, LogRegistrationTransport>()
    .AddSingleton<IContextPropertiesProvider, ContextPropertiesProvider>()
    .Configure<LogAgentOptions>(builder.Configuration.GetSection("LogAgent"));

var app = builder.Build();

// Configure the HTTP request pipeline.


app.Run();

