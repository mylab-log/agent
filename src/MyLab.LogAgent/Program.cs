using MyLab.LogAgent;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddLogAgentLogic()
    .ConfigureLogAgentLogic(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.


app.Run();

