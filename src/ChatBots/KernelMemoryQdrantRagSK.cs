using ChatBots.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatBots;

internal class KernelMemoryQdrantRagSK
{
    private static async Task<string> GetLongTermMemory(IKernelMemory memory, string query, bool asChunks = true)
    {
        if (asChunks)
        {
            // Fetch raw chunks, using KM indexes. More tokens to process with the chat history, but only one LLM request.
            SearchResult memories = await memory.SearchAsync(query, limit: 10);
            return memories.Results.SelectMany(m => m.Partitions).Aggregate("", (sum, chunk) => sum + chunk.Text + "\n").Trim();
        }

        // Use KM to generate an answer. Fewer tokens, but one extra LLM request.
        MemoryAnswer answer = await memory.AskAsync(query);
        return answer.Result.Trim();
    }

    private static async Task ChatLoop(Kernel kernel, IKernelMemory memory,
        string systemPrompt, bool isStreaming, 
        PromptExecutionSettings settings = null)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory(systemPrompt);

        // Start the chat
        var assistantMessage = "Hello, how can I help?";
        Console.WriteLine($"Assistant> {assistantMessage}\n");
        chatHistory.AddAssistantMessage(assistantMessage);

        // Infinite chat loop
        var reply = new StringBuilder();

        while (true)
        {
            // Get user message (retry if the user enters an empty string)
            Console.Write("You> ");
            var userMessage = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(userMessage)) { break; }
            else { chatHistory.AddUserMessage(userMessage); }

            // Recall relevant information from memory
            var longTermMemory = await GetLongTermMemory(memory, userMessage);
            // Console.WriteLine("-------------------------- recall from memory\n{longTermMemory}\n--------------------------");

            // Inject the memory recall in the initial system message
            chatHistory[0].Content = $"{systemPrompt}\n\nLong term memory:\n{longTermMemory}";

            reply.Clear();

            // Get the response from the AI
            if (isStreaming)
            {
                Console.Write("\nAssistant> ");

                await foreach (StreamingChatMessageContent stream in chatService.GetStreamingChatMessageContentsAsync(
                    chatHistory, executionSettings: settings, kernel: kernel))
                {
                    Console.Write(stream.Content);
                    reply.Append(stream.Content);
                }
            }
            else
            {
                var aiReply = await chatService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings: settings,
                    kernel: kernel);

                Console.WriteLine("Assistant> " + aiReply);

                // Add the message from the agent to the chat history
                chatHistory.AddMessage(aiReply.Role, aiReply.Content ?? string.Empty);
            }

            chatHistory.AddAssistantMessage(reply.ToString());
            Console.WriteLine("\n");
        }
    }

    private static async Task MemorizeDocumentsAsync(IKernelMemory memory, List<string> pages)
    {
        Func<string, string> GetUrlId = url => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).ToUpperInvariant();

        foreach (var url in pages)
        {
            var id = GetUrlId(url);

            // Check if the page is already in memory, to avoid importing twice
            if (!await memory.IsDocumentReadyAsync(id))
                try
                {
                    await memory.ImportWebPageAsync(url, documentId: id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed for {url} with {ex.Message}");
                }
        }
    }

    private static async Task MemorizeTextAsync(IKernelMemory memory, (string, string)[] facts)
    {
        foreach (var fact in facts)
        {
            if (!await memory.IsDocumentReadyAsync(fact.Item1))
                try
                {
                    await memory.ImportTextAsync(fact.Item2, documentId: fact.Item1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed for {fact.Item1} with {ex.Message}");
                }
        }
    }

    public static async Task RagDocumentsScenarioAsync(string textModel, string embeddingModelName)
    {
        //https://github.com/microsoft/kernel-memory/blob/main/examples/302-dotnet-sk-km-chat/Program.cs
        var kernel = SemanticKernelHelper.GetKernel(textModel,
            (b, c) =>
            {
                b.AddQdrantVectorStore("localhost");
            });

        var config = new OllamaConfig
        {
            Endpoint = "http://localhost:11434",
            TextModel = new OllamaModelConfig(textModel, maxToken: 131072),
            EmbeddingModel = new OllamaModelConfig(embeddingModelName, maxToken: 2048) // nomic-embed-text
        };

        IKernelMemory memory = new KernelMemoryBuilder()
            .WithOllamaTextGeneration(config, new GPT4oTokenizer())
            .WithOllamaTextEmbeddingGeneration(config, new GPT4oTokenizer())
            .Configure(builder => builder.Services.AddLogging(l =>
            {
                l.SetMinimumLevel(LogLevel.Debug);
                l.AddSimpleConsole(c => c.SingleLine = true);
            }))
            .WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Disk }) // local file storage
            .WithQdrantMemoryDb(endpoint: "http://localhost:6333", apiKey: "x")
            .Build<MemoryServerless>();

        //https://github.com/microsoft/kernel-memory/blob/main/examples/302-dotnet-sk-km-chat/Program.cs
        List<string> documentation = [
            "https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md",
            "https://microsoft.github.io/kernel-memory/quickstart",
            "https://microsoft.github.io/kernel-memory/quickstart/configuration",
            "https://microsoft.github.io/kernel-memory/quickstart/start-service",
            "https://microsoft.github.io/kernel-memory/quickstart/python",
            "https://microsoft.github.io/kernel-memory/quickstart/csharp",
            "https://microsoft.github.io/kernel-memory/quickstart/java",
            "https://microsoft.github.io/kernel-memory/quickstart/javascript",
            "https://microsoft.github.io/kernel-memory/quickstart/bash",
            "https://microsoft.github.io/kernel-memory/service",
            "https://microsoft.github.io/kernel-memory/service/architecture",
            "https://microsoft.github.io/kernel-memory/serverless",
            "https://microsoft.github.io/kernel-memory/security/filters",
            "https://microsoft.github.io/kernel-memory/how-to/custom-partitioning",
            "https://microsoft.github.io/kernel-memory/concepts/indexes",
            "https://microsoft.github.io/kernel-memory/concepts/document",
            "https://microsoft.github.io/kernel-memory/concepts/memory",
            "https://microsoft.github.io/kernel-memory/concepts/tag",
            "https://microsoft.github.io/kernel-memory/concepts/llm",
            "https://microsoft.github.io/kernel-memory/concepts/embedding",
            "https://microsoft.github.io/kernel-memory/concepts/cosine-similarity",
            "https://microsoft.github.io/kernel-memory/faq",
            "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/README.md",
            "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/dotnet/README.md",
            "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/python/README.md",
            "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/java/README.md",
            "https://learn.microsoft.com/en-us/semantic-kernel/overview/",
            "https://learn.microsoft.com/en-us/semantic-kernel/get-started/quick-start-guide",
            "https://learn.microsoft.com/en-us/semantic-kernel/agents/"
        ];

        if (!await memory.IsDocumentReadyAsync("help"))
            await memory.ImportTextAsync("We can talk about Semantic Kernel and Kernel Memory, you can ask any questions, I will try to reply using information from public documentation in Github",
                documentId: "help");

        Console.WriteLine("# Saving documentation into kernel memory...");
        await MemorizeDocumentsAsync(memory, documentation);

        // Chat setup
        var systemPrompt = """
            You are a helpful assistant replying to user questions using information from your memory.
            Reply very briefly and concisely, get to the point immediately. Don't provide long explanations unless necessary.
            Sometimes you don't have relevant memories so you reply saying you don't know, don't have the information.
            The topic of the conversation is Kernel Memory (KM) and Semantic Kernel (SK).
            """;

        // Infinite chat loop
        Console.WriteLine("# Starting chat...");
        await ChatLoop(kernel, memory, systemPrompt, isStreaming: true);
    }

    public static async Task ClinicScenarioAsync(string textModel, string embeddingModelName)
    {
        // https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Demos/VectorStoreRAG/README.md
        // https://github.com/microsoft/kernel-memory/blob/main/extensions/Qdrant/Qdrant.TestApplication/Program.cs

        // https://github.com/microsoft/kernel-memory/blob/main/examples/212-dotnet-ollama/Program.cs

        //https://github.com/microsoft/semantic-kernel/blob/21f8a278e55c23b34e96c9de3ab06e8564dca703/docs/decisions/00NN-text-search.md
        //https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Demos/VectorStoreRAG/README.md

        var kernel = SemanticKernelHelper.GetKernel(textModel,
            (b, c) =>
            {
                //b.AddInMemoryVectorStore();

                //b.Services.AddSingleton<QdrantClient>(sp => new QdrantClient("localhost"));
                //b.AddQdrantVectorStore();
                b.AddQdrantVectorStore("localhost");
            });

        kernel.Plugins.AddFromType<ClinicPlugins>("Clinic");

        var config = new OllamaConfig
        {
            Endpoint = "http://localhost:11434",
            TextModel = new OllamaModelConfig(textModel, maxToken: 131072), // mistral, calebfahlgren/natural-functions, granite-code:8b ?
            EmbeddingModel = new OllamaModelConfig(embeddingModelName, maxToken: 2048)
        };

        //var embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
        //var handler = new HttpClientHandler();
        //handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        //handler.ServerCertificateCustomValidationCallback =
        //    (httpRequestMessage, cert, cetChain, policyErrors) =>
        //    {
        //        return true;
        //    };
        //var client = new HttpClient(handler);
        //var memoryStore = new QdrantMemoryStore(httpClient: client, vectorSize: 1536, endpoint: "http://localhost:6333");
        //var memory = new SemanticTextMemory(memoryStore, embeddingGenerator);

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

        Console.WriteLine("# Saving facts into kernel memory...");
        await MemorizeTextAsync(memory, new[]
        {
            //("dayOfWeek", "Today is friday"),
            ("timing1", "Clinic opens in morning from 10AM to 1PM, Mondays, Tuesdays, Wednesdays, Thursdays, Fridays and Saturdays"),
            ("timing2", "Clinic opens in evening from 6PM to 8PM, Mondays, Tuesdays and Wednesdays only, for the rest of the week clinic is off in evening"),
            ("timing3", "Clinic is off on Sunday")
        });

        OpenAIPromptExecutionSettings settings = new()
        {
            //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions, // this cant be used together with FunctionChoiceBehavior
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        //var prompt = @"
        //    Question: {{$input}}
        //    Kernel Memory Answer: {{memory.ask}}
        //    Answer the question using the memory content: {{Recall}}";
        //var systemPrompt = """
        //    Question: {{$input}}
        //    Tool call result: {{memory.ask $input}}
        //    If the answer is empty say "I don't know", otherwise answer the question using the tool call result; be succinct and if needed call another tool / function
        //    """;
        var systemPrompt = """
            You are the clinic assistant who guides and answers the patients.

            Question: {{$input}}
            Answer the question using the tool call results; be succinct and if you feel like it call another tool / function
            """;

        // https://github.com/microsoft/kernel-memory/blob/main/examples/003-dotnet-SemanticKernel-plugin/Program.cs
        var promptOptions = new OpenAIPromptExecutionSettings { ChatSystemPrompt = "Answer or say \"I don't know\".", MaxTokens = 100, Temperature = 0, TopP = 0 };

        ////https://medium.com/@johnkane24/local-memory-c-semantic-kernel-ollama-and-sqlite-to-manage-chat-memories-locally-9b779fc56432
        //var getArguments = new Func<string, KernelArguments>(
        //    q => new KernelArguments(settings)
        //    {
        //        { "input", q },
        //        { "collection", MemoryCollectionName }
        //    });

        //var question = "Will clinic open later in the day today?";
        //var resultFunction = await kernel.InvokePromptAsync(prompt, getArguments(question));
        //Console.WriteLine(resultFunction);

        // Infinite chat loop
        Console.WriteLine("# Starting chat...");
        await ChatLoop(kernel, memory, systemPrompt, isStreaming: false, settings);
    }
}
