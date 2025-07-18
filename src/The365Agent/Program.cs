using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Threading;
using The365Agent;
using The365Agent.Bot.Agents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient("WebClient", client => client.Timeout = TimeSpan.FromSeconds(600));
builder.Services.AddHttpContextAccessor();
builder.Logging.AddConsole();

// Register Semantic Kernel
builder.Services.AddKernel();

// Register the AI service of your choice. AzureOpenAI and OpenAI are demonstrated...
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
        modelId: config.OpenAI.DefaultModel,
        apiKey: config.OpenAI.ApiKey);

// Register the WeatherForecastAgent
builder.Services.AddTransient<WeatherForecastAgent>();

// Add AspNet token validation
builder.Services.AddBotAspNetAuthentication(builder.Configuration);

// Register IStorage.  For development, MemoryStorage is suitable.
// For production Agents, persisted storage should be used so
// that state survives Agent restarts, and operate correctly
// in a cluster of Agent instances.
builder.Services.AddSingleton<IStorage, MemoryStorage>();

// Add AgentApplicationOptions from config.
builder.AddAgentApplicationOptions();

// Add AgentApplicationOptions.  This will use DI'd services and IConfiguration for construction.
builder.Services.AddTransient<AgentApplicationOptions>();

// Add the bot (which is transient)
builder.AddAgent<The365Agent.Bot.WeatherAgentBot>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
{
    await adapter.ProcessAsync(request, response, agent, cancellationToken);
});

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Playground")
{
    app.MapGet("/", () => "Weather Bot");
    app.UseDeveloperExceptionPage();
    app.MapControllers().AllowAnonymous();
}
else
{
    app.MapControllers();
}

app.Run();

