#pragma warning disable SKEXP0020

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
using System.Threading.Tasks;

namespace ChatBots;

internal class KernelMemoryPgRagSK
{
    private static async Task CreateDatabase(string connectionString, string databaseName)
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

    private static async Task ConfigureVectors(string connectionString)
    {
        using var connection = new NpgsqlConnection(connectionString);
        using var command = new NpgsqlCommand();

        connection.Open();
        command.Connection = connection;
        command.CommandText = "CREATE EXTENSION IF NOT EXISTS vector";
        await command.ExecuteNonQueryAsync();

        connection.Close();
    }

    public static async Task ClinicScenarioAsync(string urlOllama, string textModel, string embeddingModelName, string initialConnectionString, string connectionString, string databaseName)
    {
        await CreateDatabase(initialConnectionString, databaseName);
        await ConfigureVectors(connectionString);

        var kernel = SemanticKernelHelper.GetKernel(urlOllama, textModel,
            (b, c) => b.Services.AddPostgresVectorStore(connectionString));
        kernel.Plugins.AddFromType<ClinicPlugins>("Clinic");

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
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Disk }) // local file storage
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
}
