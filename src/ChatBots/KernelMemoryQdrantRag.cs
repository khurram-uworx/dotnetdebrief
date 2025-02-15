using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatBots;

class KernelMemoryQdrantRag
{
    public static async Task RagWikipediaScenarioAsync(string urlOllama, string textModel, string embeddingModelName, string urlQdrant)
    {
        // https://github.com/microsoft/kernel-memory/tree/main/examples/002-dotnet-Serverless  
        // https://github.com/microsoft/kernel-memory/blob/main/examples/212-dotnet-ollama/Program.cs

        /*
         * We can use Semantic Kernel for Text Generation and Embedding
         * even the Hugging Face Models through it: https://github.com/microsoft/kernel-memory/discussions/753
         */

        //https://github.com/microsoft/kernel-memory/blob/main/examples/212-dotnet-ollama/Program.cs
        var config = new OllamaConfig
        {
            Endpoint = urlOllama,
            TextModel = new OllamaModelConfig(textModel, maxToken: 131072),
            EmbeddingModel = new OllamaModelConfig(embeddingModelName, maxToken: 2048) // nomic-embed-text
        };

        IKernelMemory memory = new KernelMemoryBuilder()
            .WithOllamaTextGeneration(config, new CL100KTokenizer()) // GPT4oTokenizer())
            .WithOllamaTextEmbeddingGeneration(config, new CL100KTokenizer()) // GPT4oTokenizer())
            .Configure(builder => builder.Services.AddLogging(l =>
            {
                l.SetMinimumLevel(LogLevel.Warning);
                l.AddSimpleConsole(c => c.SingleLine = true);
            }))
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Disk }) // local file storage
            .WithQdrantMemoryDb(endpoint: urlQdrant, apiKey: "x")
            .Build<MemoryServerless>();

        Console.WriteLine("# Generating kernel memory...");

        Func<string, Guid> getUrlId = url =>
        {
            //Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).ToUpperInvariant();
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
                return new Guid(hash);
            }
        };

        if (!await memory.IsDocumentReadyAsync(getUrlId("help").ToString(), index: "wiki"))
            await memory.ImportTextAsync("We can talk about selected Wikipedia pages, you can ask any questions, I will try to reply using the information I have",
                documentId: getUrlId("help").ToString(), index: "wiki");

        foreach (var url in new[] {
            "https://en.wikipedia.org/wiki/Avicenna",
            "https://en.wikipedia.org/wiki/Al-Farabi" })
        {
            var id = getUrlId(url);

            // Check if the page is already in memory, to avoid importing twice
            if (await memory.IsDocumentReadyAsync(id.ToString(), index: "wiki"))
                Console.WriteLine($"# {url} having {id} is already memorized");
            else
                try
                {
                    Console.WriteLine($"# Memorizing {url} with {id}");
                    await memory.ImportWebPageAsync(url, documentId: id.ToString(), index: "wiki");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"# Failed for {url} with {ex.Message}");
                }
        }

        //while (true)
        {
            var question = "Who was persian? Be succinct; just give the name and dont give any other detail!!!";
            Console.WriteLine($"You> {question}");
            //var question = "Who was persian?"; //Console.ReadLine();
            //if (question == "break" || question == "exit" || question == null) break;

            var answer = await memory.AskAsync(question, index: "wiki");
            Console.Write("Assistant> ");
            Console.WriteLine(answer.Result);
        }
    }
}
