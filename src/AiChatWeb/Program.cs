using Microsoft.Extensions.AI;
using AiChatWeb.Components;
using AiChatWeb.Services;
using AiChatWeb.Services.Ingestion;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var qdrantEndpoint = builder.Configuration["AI__Qdrant__Endpoint"] ?? "localhost";
var ollamaEndpoint = builder.Configuration["AI__Ollama__Endpoint"] ?? "http://localhost:11434";
var chatModel = builder.Configuration["AI__Ollama__ChatModel"] ?? "llama3.2";
var embeddingModel = builder.Configuration["AI__Ollama__EmbeddingModel"] ?? "all-minilm";

builder.Services.AddTransient<IChatClient>(sp =>
{
    return new OllamaChatClient(ollamaEndpoint, modelId: chatModel)
        .AsBuilder()
        .UseFunctionInvocation()
        //.UseOpenTelemetry(configure: c =>
        //    c.EnableSensitiveData = builder.Environment.IsDevelopment());
        .Build();
});
builder.Services.AddTransient<IEmbeddingGenerator>(sp =>
{
    return new OllamaEmbeddingGenerator(ollamaEndpoint, modelId: embeddingModel)
        .AsBuilder()
        .Build();
});
builder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient(qdrantEndpoint));

builder.Services.AddQdrantCollection<Guid, IngestedChunk>("data-aichatweb-chunks");
builder.Services.AddQdrantCollection<Guid, IngestedDocument>("data-aichatweb-documents");
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from
// other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be reflected back
// to users or could be a source of prompt injection risk.
await DataIngestor.IngestDataAsync(
    app.Services,
    new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

app.Run();
