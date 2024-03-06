using MyLab.HttpMetrics;
using MyLab.Log;
using MyLab.LogAgent;
using MyLab.LogAgent.Tools;
using MyLab.Search.EsAdapter;
using MyLab.Search.EsAdapter.Inter;
using MyLab.StatusProvider;
using MyLab.WebErrors;
using Newtonsoft.Json;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLogAgentLogic()
    .AddEsTools()
    .ConfigureLogAgentLogic(builder.Configuration)
    .ConfigureEsTools(builder.Configuration)
    .ConfigureEsTools(opt =>
    {
        opt.SerializerFactory = new CombineEsSerializerFactory(CreateJsonSerializer());
    })
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

static JsonSerializer CreateJsonSerializer()
{
    return new JsonSerializer
    {
        Converters =
        {
            new DockerLabelNameJsonConverter()
        }
    };
}