using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatBots;

static class Program
{
    static async Task Main(string[] args)
    {
        var urlOllama = "http://localhost:11434";
        /*
            * all-minilm all-MiniLM-L6-v2
            *      Best for lightweight, fast, and general-purpose use cases where resource efficiency is critical
            * nomic-embed-text / nomic-embed-large
            *      A balanced choice for general-purpose tasks with a focus on transparency and ethical AI
            * mxbai-embed-text / mxbai-embed-large
            *      Ideal for high-performance, multilingual, and complex tasks where accuracy and richness of
            *      embeddings are prioritized over speed and resource usage
            */
        var embeddingModelName = "all-minilm";
        /*
            * phi3/phi3:mini/phi3:latest
            * llama2 and phi3 dont support tools
            *      calebfahlgren/natural-functions
            * mistral, llama3.2
            * andthattoo/tinyagent-1.1b doesnt support tool calling
            */
        var textModel = "qwen3:4b"; // "qwen2.5:3b";

        var options = new Dictionary<int, Action>();
        Console.WriteLine("Ensure your Ollama is up; choose your chat bot");

        Console.WriteLine("[1] OpenAI Library Chat Completion");
        options.Add(1, () => OpenAI.Run(urlOllama, textModel));

        Console.WriteLine("[2] DirectML Chat Completion");
        options.Add(2, () => ML.MLTest(textModel: "directml-int4-awq-block-128"));

        Console.WriteLine("[3] OptionAI Tools");
        options.Add(3, () => OpenAITools.ChatWithTools(urlOllama, textModel));

        Console.WriteLine("[4] AutoGen Hello World");
        options.Add(4, () => AutoGenChats.HelloOllamaWorldAsync(urlOllama, textModel).Wait());
        Console.WriteLine("[5] AutoGen Hello Agents");
        options.Add(5, () => AutoGenChats.HelloAgents(urlOllama, textModel).Wait());
        //https://microsoft.github.io/autogen-for-net/articles/Built-in-messages.html

        Console.WriteLine("[11] Semantic Kernel (SK) Hello World");
        options.Add(11, () => SemanticKernelChats.HelloWorldAsync(urlOllama, textModel).Wait());
        Console.WriteLine("[12] SK Prompt Scenario");
        options.Add(12, () => SemanticKernelChats.PromptScenarioAsync(urlOllama, textModel).Wait());
        Console.WriteLine("[13] SK Plugin; try Please toggle all the lights");
        options.Add(13, () => SemanticKernelChats.LightsPluginAsync(urlOllama, textModel).Wait());
        Console.WriteLine("[14] SK RAG Scenario");
        options.Add(14, () => SemanticKernelChats.RagScenarioAsync(urlOllama, textModel).Wait());

        Console.WriteLine("[21] AI Extensions Structured Output");
        options.Add(21, () => AIExtensions.StructuredOutputAsync(urlOllama, textModel).Wait());
        Console.WriteLine("[22] AI Extensions Function Calling");
        options.Add(22, () => AIExtensions.FunctionCallingAsync(urlOllama, textModel).Wait());
        //Console.WriteLine("[23] AI Extensions Chat Completition and Tool");
        //options.Add(23, () => AIExtensionTools.ChatWithAIExtensionAsync(urlOllama, textModel).Wait());

        Console.WriteLine("[31] SK Agents - Ticket Handler");
        options.Add(31, () =>
        {
            var ticket = """
                Internet connection is very slow, I think its the Saturday's rain, something has gone wrong since then.
                It takes lot of time to download things, sometimes page is not completely loaded. My office operations
                are suffering because of this. Please do needful as soon as possible.
                
                I am your premium customer and my service/connection id is khurram-uworx
            """;
            new AgentTicketHandler(urlOllama, textModel).HandleTicketAsync(ticket).Wait();
        });
        Console.WriteLine("[32] SK Agents - Philophers");
        options.Add(32, () => new AgentDebate(urlOllama, textModel).DebateAsync("How can we ensure that AI benefits all of humanity?").Wait());
        Console.WriteLine("[33*] SK Agents - Creative Writer; not completed / working");
        options.Add(33, () => new AgentCreativeWriter(urlOllama, textModel).WriteCreativelyAsync("How can we ensure that AI benefits all of humanity?").Wait());

        Console.WriteLine();
        Console.WriteLine("[41] MCP: Github Repository");
        options.Add(41, () => new McpRepository(urlOllama, textModel).HandleMcpPromptAsync(
            "Summarize the last four commits to the microsoft/semantic-kernel repository?").Wait());
        Console.WriteLine("[42] MCP: Playwright");
        options.Add(42, () => new McpPlaywright(urlOllama, textModel).HandleMcpPromptAsync(
            "Browse to https://uworx.webhr.co/jobs/home using the Playwright tool and then summarize currently opened jobs you find on that web page").Wait());

        Console.WriteLine();
        Console.WriteLine("Run Qdrant first; docker run --rm -p 6333:6333 -p 6334:6334 qdrant/qdrant");
        var urlQdrant = "http://localhost:6333";
        var hostQdrant = "localhost";

        Console.WriteLine("[51] Qdrant Quick Start, test_collection will be created/used");
        options.Add(51, () => Qdrant.QuickStartAsync(hostQdrant, collectionName: "test_collection").Wait());
        Console.WriteLine("[52] Qdrant with Vector Data Extensions, movies collection will be created/used");
        options.Add(52, () => Qdrant.VectorDataExtensionsAsync(urlOllama, embeddingModelName, hostQdrant, collectionName: "movies").Wait());
        Console.WriteLine("[53] Qdrant RAG Scenario - Clinic");
        options.Add(53, () => QdrantSemanticKernel.RagClinicScenarioAsync(urlOllama, textModel, urlQdrant, hostQdrant, collectionName: "clinicFacts").Wait());
        Console.WriteLine("[54] Qdrant Semantic Search, movies collection will be created/used");
        options.Add(54, () => QdrantSemanticKernel.SearchScenarioAsync(urlOllama, textModel, embeddingModelName, hostQdrant, collectionName: "movies").Wait());
        Console.WriteLine("[55] Qdrant as Semantic Kernel Memory; RAG for wikipedia");
        options.Add(55, () => KernelMemoryQdrantRag.RagWikipediaScenarioAsync(urlOllama, textModel, embeddingModelName, urlQdrant).Wait());
        Console.WriteLine("[56] Qdrant as Semantic Kernel Memory; RAG for web pages (SK docs)");
        options.Add(56, () => KernelMemoryQdrantRagSK.RagDocumentsScenarioAsync(urlOllama, textModel, embeddingModelName, urlQdrant, hostQdrant).Wait());

        Console.WriteLine();
        Console.WriteLine("Run pgvector first; docker run -e POSTGRES_PASSWORD=uworx -p 5432:5432 pgvector/pgvector:pg17");
        var initialConnectionString = "Server=127.0.0.1;Port=5432;Database=postgres;User Id=postgres;Password=uworx;";

        Console.WriteLine("[61] Kernel Memory with PostgreSQL and RAG Scenario - Clinic using Semantic Kernel");
        options.Add(61, () => KernelMemoryPgRagSK.ClinicScenarioAsync(urlOllama, textModel, embeddingModelName,
            initialConnectionString,
            connectionString: "Server=127.0.0.1;Port=5432;Database=ragClinic;User Id=postgres;Password=uworx;",
            databaseName: "ragClinic").Wait());
        Console.WriteLine("[62] Kernel Memory with PostgreSQL and RAG Scenario - Resumes");
        options.Add(62, () =>
        {
            Console.Write("Enter the path where resume files exist\t");
            string resumePath = Console.ReadLine();
            KernelMemoryPgRagSK.ResumesScenarioAsync(urlOllama, textModel, embeddingModelName,
                resumePath, initialConnectionString,
                connectionString: "Server=127.0.0.1;Port=5432;Database=ragResumes;User Id=postgres;Password=uworx;",
                databaseName: "ragResumes").Wait();
        });

        Console.WriteLine();
        int option;
        if (int.TryParse(Console.ReadLine(), out option) && options.ContainsKey(option))
            options[option]();
        else
            Console.WriteLine("Invalid option");

        //https://learn.microsoft.com/en-us/semantic-kernel/how-to/vector-store-connectors/vector-store-data-ingestion
        //https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/TextChunkingAndEmbedding.cs
    }
}
