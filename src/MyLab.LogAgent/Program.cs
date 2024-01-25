using MyLab.HttpMetrics;
using MyLab.LogAgent;
using MyLab.Search.EsAdapter;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLogAgentLogic()
    .AddEsTools()
    .ConfigureLogAgentLogic(builder.Configuration)
    .ConfigureEsTools(builder.Configuration)
    .AddUrlBasedHttpMetrics();

var app = builder.Build();

app.UseUrlBasedHttpMetrics();

app.Run();

