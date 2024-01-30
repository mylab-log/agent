using MyLab.HttpMetrics;
using MyLab.Log;
using MyLab.LogAgent;
using MyLab.Search.EsAdapter;
using MyLab.WebErrors;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLogAgentLogic()
    .AddEsTools()
    .ConfigureLogAgentLogic(builder.Configuration)
    .ConfigureEsTools(builder.Configuration)
    .AddUrlBasedHttpMetrics()
    .AddLogging(b => b.AddMyLabConsole())
    .AddControllers(c => c.AddExceptionProcessing());

var app = builder.Build();

app.UseUrlBasedHttpMetrics();
app.MapControllers();

app.Run();

