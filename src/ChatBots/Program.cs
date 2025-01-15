using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatBots;

internal static class Program
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
         * mistral
         */
        var textModel = "llama3.2";

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

        Console.WriteLine("[21] AI Extensions Chat Completition and Tool");
        options.Add(21, () => AIExtensionTools.ChatWithAIExtensionAsync(urlOllama, textModel).Wait());

        Console.WriteLine("Run Qdrant first; docker run --rm -p 6333:6333 -p 6334:6334 qdrant/qdrant");
        var urlQdrant = "http://localhost:6333";
        var hostQdrant = "localhost";

        Console.WriteLine("[31] Qdrant Quick Start, test_collection will be created/used");
        options.Add(31, () => Qdrant.QuickStartAsync(hostQdrant, collectionName: "test_collection").Wait());
        Console.WriteLine("[32] Qdrant with Vector Data Extensions, movies collection will be created/used");
        options.Add(32, () => Qdrant.VectorDataExtensionsAsync(urlOllama, embeddingModelName, hostQdrant, collectionName: "movies").Wait());
        Console.WriteLine("[33] Qdrant RAG Scenario - Clinic");
        options.Add(33, () => QdrantSemanticKernel.RagClinicScenarioAsync(urlOllama, textModel, urlQdrant, hostQdrant, memoryCollectionName: "timingFacts").Wait());
        Console.WriteLine("[34] Qdrant Semantic Search, movies collection will be created/used");
        options.Add(34, () => QdrantSemanticKernel.SearchScenarioAsync(urlOllama, textModel, embeddingModelName, hostQdrant, collectionName: "movies").Wait());
        Console.WriteLine("[35] Qdrant as Semantic Kernel Memory; RAG for wikipedia");
        options.Add(35, () => KernelMemoryQdrantRag.RagWikipediaScenarioAsync(urlOllama, textModel, embeddingModelName, urlQdrant).Wait());
        Console.WriteLine("[36] Qdrant as Semantic Kernel Memory; RAG for web pages (SK docs)");
        options.Add(36, () => KernelMemoryQdrantRagSK.RagDocumentsScenarioAsync(urlOllama, textModel, embeddingModelName, urlQdrant, hostQdrant).Wait());
        Console.WriteLine("[37] Kernel Memory with Qdrant and RAG Scenario - Clinic using Semantic Kernel");
        options.Add(37, () => KernelMemoryQdrantRagSK.ClinicScenarioAsync(urlOllama, textModel, embeddingModelName, urlQdrant, hostQdrant).Wait());

        int option;
        if (int.TryParse(Console.ReadLine(), out option) && options.ContainsKey(option))
            options[option]();
        else
            Console.WriteLine("Invalid option");

        //https://learn.microsoft.com/en-us/semantic-kernel/how-to/vector-store-connectors/vector-store-data-ingestion
        //https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/TextChunkingAndEmbedding.cs
    }
}
