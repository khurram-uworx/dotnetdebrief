using ChatBots.Helpers;
using ChatBots.Plugins;
using Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.SemanticKernel;
using Npgsql;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBots;

class KernelMemoryPgRagSK
{
    static async Task CreateDatabase(string connectionString, string databaseName)
    {
        using var connection = new NpgsqlConnection(connectionString);
        using var command = new NpgsqlCommand();
        
        connection.Open();
        command.Connection = connection;
        command.CommandText = $"SELECT EXISTS(SELECT datname FROM pg_catalog.pg_database WHERE datname = '{databaseName}');";

        var dbExists = await command.ExecuteScalarAsync();
        if (dbExists is not null && dbExists is false)
        {
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        connection.Close();
    }

    static async Task ConfigureVectors(string connectionString)
    {
        using var connection = new NpgsqlConnection(connectionString);
        using var command = new NpgsqlCommand();

        connection.Open();
        command.Connection = connection;
        command.CommandText = "CREATE EXTENSION IF NOT EXISTS vector";
        await command.ExecuteNonQueryAsync();

        connection.Close();
    }

    public static async Task ClinicScenarioAsync(string urlOllama, string textModel, string embeddingModelName,
        string initialConnectionString, string connectionString, string databaseName)
    {
        await CreateDatabase(initialConnectionString, databaseName);
        await ConfigureVectors(connectionString);

        var kernel = SemanticKernelHelper.GetKernel(urlOllama, textModel,
            (b, c) => b.Services.AddPostgresVectorStore(connectionString));
        kernel.Plugins.AddFromType<ClinicPlugin>("Clinic");

        var config = new OllamaConfig
        {
            Endpoint = urlOllama,
            TextModel = new OllamaModelConfig(textModel, maxToken: 131072),
            EmbeddingModel = new OllamaModelConfig(embeddingModelName, maxToken: 2048)
        };

        IKernelMemory memory = new KernelMemoryBuilder()
            .WithOllamaTextGeneration(config, new GPT4oTokenizer())
            .WithOllamaTextEmbeddingGeneration(config, new GPT4oTokenizer())
            .Configure(builder => builder.Services.AddLogging(l =>
            {
                l.SetMinimumLevel(LogLevel.Warning);
                l.AddSimpleConsole(c => c.SingleLine = true);
            }))
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Disk, Directory = "MemoryClinic" }) // local file storage
            .WithPostgresMemoryDb(connectionString)
            .Build<MemoryServerless>();

        Console.WriteLine("# Saving facts into kernel memory...");
        await memory.MemorizeTextAsync(new[]
        {
            ("timing1", "Clinic opens in morning from 10AM to 1PM, Mondays, Tuesdays, Wednesdays, Thursdays, Fridays and Saturdays"),
            ("timing2", "Clinic opens in evening from 6PM to 8PM, Mondays, Tuesdays and Wednesdays only, for the rest of the week clinic is off in evening"),
            ("timing3", "Clinic is off on Sunday")
        });

        PromptExecutionSettings settings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var systemPrompt = """
            You are the clinic assistant who guides and answers the patients.
            Today is friday; dont check for this.

            Question: {{$input}}
            Answer the question using the tool call results; be succinct and if you feel like it call another tool / function
            """;

        Console.WriteLine("# Starting chat...");
        await kernel.ChatLoop(memory, systemPrompt, isStreaming: false, settings);
    }

    public static async Task ResumesScenarioAsync(string urlOllama, string textModel, string embeddingModelName,
        string resumePath,
        string initialConnectionString, string connectionString, string databaseName)
    {
        await CreateDatabase(initialConnectionString, databaseName);
        await ConfigureVectors(connectionString);

        var kernel = SemanticKernelHelper.GetKernel(urlOllama, textModel,
            (b, c) => b.Services.AddPostgresVectorStore(connectionString));

        var config = new OllamaConfig
        {
            Endpoint = urlOllama,
            TextModel = new OllamaModelConfig(textModel, maxToken: 131072),
            EmbeddingModel = new OllamaModelConfig(embeddingModelName, maxToken: 2048)
        };

        IKernelMemory memory = new KernelMemoryBuilder()
            .WithOllamaTextGeneration(config, new GPT4oTokenizer())
            .WithOllamaTextEmbeddingGeneration(config, new GPT4oTokenizer())
            .Configure(builder => builder.Services.AddLogging(l =>
            {
                l.SetMinimumLevel(LogLevel.Warning);
                l.AddSimpleConsole(c => c.SingleLine = true);
            }))
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Disk, Directory = "MemoryResumes" }) // local file storage
            .WithPostgresMemoryDb(connectionString)
            .Build<MemoryServerless>();

        Console.WriteLine("# Memorizing pdfs...");
        int total = 0, successful = 0;

        foreach (var file in Directory.GetFiles(resumePath).Select(f => new FileInfo(f)))
        {
            total++;
            try
            {
                string cleanedId = new string(file.Name.Where(char.IsLetter).ToArray());
                string documentId = await memory.ImportDocumentAsync(file.FullName, cleanedId);
                successful++;
                Console.WriteLine($"{file.Name} processed, {successful}/{total} done");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process {file.Name}");
                Console.WriteLine(ex);
            }
        }

        var systemPrompt = """
            You are an AI assistant that answers questions using documents stored in the connected memory/vector store.
            Guidelines:
            - When providing an answer based on retrieved documents, always include a reference to the document ID.
            - Keep responses clear and concise.
            - If necessary, call additional tools/functions to improve the accuracy or completeness of your answer.

            Question: {{$input}}
            Answer: (Provide response using retrieved data, ensuring document ID references.)
            """;

        Console.WriteLine("# Starting chat...");
        await kernel.ChatLoop(memory, systemPrompt, isStreaming: false);
    }
}
