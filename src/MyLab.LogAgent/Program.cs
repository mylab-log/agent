using MyLab.LogAgent;
using MyLab.Search.EsAdapter;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddLogAgentLogic()
    .AddEsTools()
    .ConfigureLogAgentLogic(builder.Configuration)
    .ConfigureEsTools(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.


app.Run();

