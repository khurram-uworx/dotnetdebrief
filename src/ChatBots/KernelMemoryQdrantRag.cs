#pragma warning disable SKEXP0050

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.AI.OpenAI;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatBots;

internal class KernelMemoryQdrantRag
{
    public static async Task RagWikipediaScenarioAsync()
    {
        var config = new OllamaConfig
        {
            Endpoint = "http://localhost:11434",
            TextModel = new OllamaModelConfig("phi3", maxToken: 131072),
            EmbeddingModel = new OllamaModelConfig("mxbai-embed-large", maxToken: 2048) // nomic-embed-text
        };

        IKernelMemory memory = new KernelMemoryBuilder()
            .WithOllamaTextGeneration(config, new GPT4oTokenizer())
            .WithOllamaTextEmbeddingGeneration(config, new GPT4oTokenizer())
            .Configure(builder => builder.Services.AddLogging(l =>
            {
                l.SetMinimumLevel(LogLevel.Warning);
                l.AddSimpleConsole(c => c.SingleLine = true);
            }))
            .WithQdrantMemoryDb(endpoint: "http://localhost:6333", apiKey: "x")
            .Build<MemoryServerless>();

        Console.WriteLine("# Generating kernel memory...");

        if (!await memory.IsDocumentReadyAsync("help"))
            await memory.ImportTextAsync("We can talk about selected Wikipedia pages, you can ask any questions, I will try to reply using the information I have",
                documentId: "help");

        Func<string, string> getUrlId = url => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).ToUpperInvariant();

        foreach (var url in new[] {
            "https://en.wikipedia.org/wiki/Avicenna",
            "https://en.wikipedia.org/wiki/Al-Farabi" })
        {
            var id = getUrlId(url);

            // Check if the page is already in memory, to avoid importing twice
            if (await memory.IsDocumentReadyAsync(id))
                Console.WriteLine($"# {url} having {id} is already memorized");
            else
                try
                {
                    Console.WriteLine($"# Memorizing {url} with {id}");
                    await memory.ImportWebPageAsync(url, documentId: id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"# Failed for {url} with {ex.Message}");
                }
        }

        while (true)
        {
            Console.Write("You> ");
            var question = Console.ReadLine();

            if (question == "break" || question == "exit" || question == null) break;

            var answer = await memory.AskAsync(question);
            Console.Write("Assistant> ");
            Console.WriteLine(answer.Result);
        }
    }
}
