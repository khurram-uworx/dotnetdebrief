using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatBots;

static class Program
{
    static async Task Main(string[] args)
    {
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
        var textModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "llama3.2"; //qwen2.5:3b, qwen3:4b
        // Phi-3-mini-128k-cpu-int4-rtn-block-32-onnx
        var urlOllama = "http://localhost:11434/v1"; //http://127.0.0.1:5272/v1  AI Toolkit
        var inferenceUrl = Environment.GetEnvironmentVariable("OPENAI_API_URL") ?? urlOllama;
        var inferenceKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "1234";

        var options = new Dictionary<int, Action>();
        Console.WriteLine("Ensure your Ollama is up; choose your chat bot");

        Console.WriteLine("[1] OllamaSharp Describe Image");
        options.Add(1, () => OllamaSharp.RunAsync(urlOllama, model: "gemma3").Wait()); // gemma3 has vision capabilities

        Console.WriteLine("[2] OpenAI Library Chat Completion");
        options.Add(2, () => OpenAI.Run(inferenceUrl, inferenceKey, textModel));
        Console.WriteLine("[3] OptionAI Tools");
        options.Add(3, () => OpenAITools.ChatWithTools(inferenceUrl, inferenceKey, textModel));

        Console.WriteLine("[4] Microsoft Agent Framework : Writer - Editor");
        options.Add(4, () => Agents.WriterEditorAsync(inferenceUrl, model: "llama3.2:1b").Wait()); // giving it a light model
        Console.WriteLine("[5] Microsoft Agent Framework : Slogan - Feedback Iterations");
        options.Add(5, () => Agents.AgentWorkflowExecutorAsync(inferenceUrl, model: "llama3.2:1b").Wait()); // giving it a light model
        Console.WriteLine("[6] Microsoft Agent Framework : Human in Loop and Checkpoint");
        options.Add(6, () => Agents.WorkflowHumanCheckpointAsync().Wait());

        Console.WriteLine("[8] AutoGen Hello World");
        options.Add(8, () => AutoGenChats.HelloOllamaWorldAsync(inferenceUrl, textModel).Wait());
        Console.WriteLine("[9] AutoGen Hello Agents");
        options.Add(9, () => AutoGenChats.HelloAgents(inferenceUrl, textModel).Wait());
        //https://microsoft.github.io/autogen-for-net/articles/Built-in-messages.html

        Console.WriteLine("[11] Semantic Kernel (SK) Hello World");
        options.Add(11, () => SemanticKernelChats.HelloWorldAsync(inferenceUrl, textModel).Wait());
        Console.WriteLine("[12] SK Prompt Scenario");
        options.Add(12, () => SemanticKernelChats.PromptScenarioAsync(inferenceUrl, textModel).Wait());
        Console.WriteLine("[13] SK Lights Plugin; try Please toggle all the lights");
        options.Add(13, () => SemanticKernelChats.LightsPluginAsync(inferenceUrl, textModel).Wait());
        Console.WriteLine("[14] SK Jira Plugin");
        options.Add(14, () => SemanticKernelChats.JiraPluginAsync(inferenceUrl, textModel).Wait());
        Console.WriteLine("[19] SK RAG Scenario");
        options.Add(19, () => SemanticKernelChats.RagScenarioAsync(inferenceUrl, textModel).Wait());

        Console.WriteLine("[21] AI Extensions Structured Output");
        options.Add(21, () => AIExtensions.StructuredOutputAsync(inferenceUrl, textModel).Wait());
        Console.WriteLine("[22] AI Extensions Function Calling");
        options.Add(22, () => AIExtensions.FunctionCallingAsync(inferenceUrl, textModel).Wait());
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
            new SkAgentTicketHandler(inferenceUrl, textModel).HandleTicketAsync(ticket).Wait();
        });
        Console.WriteLine("[32] SK Agents - Philophers");
        options.Add(32, () => new SkAgentDebate(inferenceUrl, textModel).DebateAsync("How can we ensure that AI benefits all of humanity?").Wait());
        Console.WriteLine("[33*] SK Agents - Creative Writer; not completed / working");
        options.Add(33, () => new SkAgentCreativeWriter(inferenceUrl, textModel).WriteCreativelyAsync("How can we ensure that AI benefits all of humanity?").Wait());

        Console.WriteLine();
        Console.WriteLine("[41] MCP: Github Repository");
        options.Add(41, () => new McpRepository(inferenceUrl, inferenceKey, textModel).HandleMcpPromptAsync(
            "Summarize the last four commits to the microsoft/semantic-kernel repository?").Wait());
        Console.WriteLine("[42] MCP: Talk to your Northwind database");
        options.Add(42, () => new McpDatabase(inferenceUrl, inferenceKey, textModel).HandleMcpPromptAsync(
            """
            You are a sales assistant, use the Database MCP to access the sales database. The schema of the database is following:

            - Customers are stored in Customers table
            - Products are stored in Products table
            - Orders are stored in Orders table

            Determine the number of customers in London
            """).Wait());
        Console.WriteLine("[43] MCP: Playwright");
        options.Add(43, () => new McpPlaywright(inferenceUrl, inferenceKey, textModel).HandleMcpPromptAsync(
            //"Summarize AI news for me related to MCP on bing news. Open first link and summarize content").Wait());
            """
            You are a browser automation assistant. Use Playwright MCP to open a browser and find the cheapest available flight.
            
            Task: Search Google Flights and find the lowest-cost flight for the following routes:
            - Lahore to Jeddah
            - Lahore to Madina
            - Islamabad to Jeddah
            - Islamabad to Madina
            
            Consider any weekday (Monday through Friday). You can choose the day that gives the cheapest fare.
            
            Your objective:
            Open Google Flights, url is https://www.google.com/travel/flights
            For each route, search for flights
            Pick the weekday that results in the lowest price
            Record the airline, price, date, and departure time
            
            Use structured steps with Playwright MCP and return a list of results sorted by price ascending
            """).Wait());
            //"Browse to https://uworx.webhr.co/jobs/home using the Playwright tool and then summarize currently opened jobs you find on that web page").Wait());

        Console.WriteLine();
        Console.WriteLine("Run Qdrant first; docker run --rm -p 6333:6333 -p 6334:6334 qdrant/qdrant");
        var urlQdrant = "http://localhost:6333";
        var hostQdrant = "localhost";

        Console.WriteLine("[51] Qdrant Quick Start, test_collection will be created/used");
        options.Add(51, () => Qdrant.QuickStartAsync(hostQdrant, collectionName: "test_collection").Wait());
        Console.WriteLine("[52] Qdrant with Vector Data Extensions, movies collection will be created/used");
        options.Add(52, () => Qdrant.VectorDataExtensionsAsync(inferenceUrl, embeddingModelName, hostQdrant, collectionName: "movies").Wait());
        //Console.WriteLine("[53] Qdrant RAG Scenario - Semantic Memory - Clinic, clinicFacts collection will be created/used"); // Its deprecated
        //options.Add(53, () => QdrantSemanticKernel.RagClinicScenarioUsingSemanticMemoryAsync(urlOllama, textModel, urlQdrant, hostQdrant, collectionName: "clinicFacts").Wait());
        Console.WriteLine("[54] Qdrant RAG Scenario - Vector Search - movies collection will be created/used");
        options.Add(54, () => QdrantSemanticKernel.RagClinicScenarioUsingVectorSearchAsync(inferenceUrl, textModel, embeddingModelName, hostQdrant, collectionName: "movies").Wait());
        Console.WriteLine("[55] Qdrant as Semantic Kernel Memory; RAG for wikipedia");
        options.Add(55, () => KernelMemoryQdrantRag.RagWikipediaScenarioAsync(inferenceUrl, textModel, embeddingModelName, urlQdrant).Wait());
        Console.WriteLine("[56] Qdrant as Semantic Kernel Memory; RAG for web pages (SK docs)");
        options.Add(56, () => KernelMemoryQdrantRagSK.RagDocumentsScenarioAsync(inferenceUrl, textModel, embeddingModelName, urlQdrant, hostQdrant).Wait());

        Console.WriteLine();
        Console.WriteLine("Run pgvector first; docker run -e POSTGRES_PASSWORD=uworx -p 5432:5432 pgvector/pgvector:pg17");
        var initialConnectionString = "Server=127.0.0.1;Port=5432;Database=postgres;User Id=postgres;Password=uworx;";

        Console.WriteLine("[61] Kernel Memory with PostgreSQL and RAG Scenario - Clinic using Semantic Kernel");
        options.Add(61, () => KernelMemoryPgRagSK.ClinicScenarioAsync(inferenceUrl, textModel, embeddingModelName,
            initialConnectionString,
            connectionString: "Server=127.0.0.1;Port=5432;Database=ragClinic;User Id=postgres;Password=uworx;",
            databaseName: "ragClinic").Wait());
        Console.WriteLine("[62] Kernel Memory with PostgreSQL and RAG Scenario - Resumes");
        options.Add(62, () =>
        {
            Console.Write("Enter the path where resume files exist\t");
            string resumePath = Console.ReadLine();
            KernelMemoryPgRagSK.ResumesScenarioAsync(inferenceUrl, textModel, embeddingModelName,
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
