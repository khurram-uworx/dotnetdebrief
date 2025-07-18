using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using TheTeamsAgent;

using Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddTeams();
builder.AddTeamsDevTools();
builder.Services.AddTransient<MainController>();

builder.Logging.AddConsole();
builder.Services.AddKernel();
builder.Services.AddMcpServer()
    .WithStreamServerTransport(EchoTool.ServerInput, EchoTool.ServerOutput)
    //.WithToolsFromAssembly();
    //.WithTools<EchoTool>();
    //WithTools<Mssql.McpServer.Tools>();
    .WithTools<ReportingTool>();

var config = builder.Configuration.Get<ConfigOptions>();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_URL")) &&
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_MODEL")) &&
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_MODEL")))

    builder.Services.AddOpenAIChatCompletion(
        modelId: Environment.GetEnvironmentVariable("OPENAI_MODEL"),
        endpoint: new Uri(Environment.GetEnvironmentVariable("OPENAI_API_URL")),
        apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    );
else
    builder.Services.AddOpenAIChatCompletion(
        modelId: "llama3.2",
        endpoint: new Uri("http://localhost:11434/v1"),
        apiKey: "x");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseTeams();
app.Run();
