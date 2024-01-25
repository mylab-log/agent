using MyLab.HttpMetrics;
using MyLab.Log;
using MyLab.LogAgent;
using MyLab.Search.EsAdapter;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLogAgentLogic()
    .AddEsTools()
    .ConfigureLogAgentLogic(builder.Configuration)
    .ConfigureEsTools(builder.Configuration)
    .AddUrlBasedHttpMetrics()
    .AddLogging(b => b.AddMyLabConsole());

var app = builder.Build();

app.UseUrlBasedHttpMetrics();

app.Run();

