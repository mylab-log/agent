using Microsoft.Extensions.Configuration;
using MyLab.HttpMetrics;
using MyLab.Log;
using MyLab.LogAgent;
using MyLab.Search.EsAdapter;
using MyLab.StatusProvider;
using MyLab.WebErrors;
using System.Reflection;
using System.Runtime.InteropServices;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLogAgentLogic()
    .AddEsTools()
    .ConfigureLogAgentLogic(builder.Configuration)
    .ConfigureEsTools(builder.Configuration)
    .AddUrlBasedHttpMetrics()
    .AddLogging(b => b.AddMyLabConsole())
    .AddAppStatusProviding(builder.Configuration)
    .AddControllers(c => c.AddExceptionProcessing());
    
var app = builder.Build();

app.UseUrlBasedHttpMetrics();
app.UseStatusApi();

app.MapControllers();
app.MapMetrics();

app.Run();

