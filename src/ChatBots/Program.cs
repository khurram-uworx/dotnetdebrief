using System.Threading.Tasks;

namespace ChatBots;

internal static class Program
{
    /*
     * all-minilm all-MiniLM-L6-v2
     *      Best for lightweight, fast, and general-purpose use cases where resource efficiency is critical
     * nomic-embed-text
     *      A balanced choice for general-purpose tasks with a focus on transparency and ethical AI
     * mxbai-embed-text / nomic-embed-large
     *      Ideal for high-performance, multilingual, and complex tasks where accuracy and richness of
     *      embeddings are prioritized over speed and resource usage
     */

    static async Task Main(string[] args)
    {
        var urlOllama = "http://localhost:11434";

        //OpenAI.Run(urlOllama, textModel: "phi3:mini");
        //ML.MLTest(textModel: "directml-int4-awq-block-128");

        //OpenAITools.ChatWithTools(urlOllama, textModel: "mistral");

        //await AutoGenChats.HelloOllamaWorldAsync(urlOllama, textModel: "phi3:latest");
        //await AutoGenChats.HelloAgents(urlOllama, textModel: "mistral");
        // https://microsoft.github.io/autogen-for-net/articles/Built-in-messages.html

        // llama2 and phi3 dont support tools
        //await SemanticKernelChats.HelloWorldAsync(urlOllama, textModel: "mistral");
        //await SemanticKernelChats.PromptScenarioAsync(urlOllama, textModel: "mistral");
        //await SemanticKernelChats.LightsPluginAsync(urlOllama, textModel: "mistral");    // User: Please toggle all the lights
        //await SemanticKernelChats.RagScenarioAsync(urlOllama, textModel: "mistral");     // not working because of Semantic kernel
        //                                                                          with ollama text embedding search does not return any value
        // https://github.com/microsoft/semantic-kernel/issues/6483

        await AIExtensionTools.ChatWithAIExtensionAsync(urlOllama, textModel: "llama3.2");

        //docker run --rm -p 6333:6333 -p 6334:6334 qdrant/qdrant
        var urlQdrant = "http://localhost:6333";
        var hostQdrant = "localhost";

        //await Qdrant.QuickStartAsync(hostQdrant, collectionName: "test_collection");
        //await Qdrant.VectorDataExtensionsAsync(urlOllama, embeddingModelName: "all-minilm", hostQdrant, collectionName: "movies");

        //await QdrantSemanticKernel.RagClinicScenarioAsync(urlOllama, textModel: "llama3.2", urlQdrant, hostQdrant, memoryCollectionName: "timingFacts");
        //await QdrantSemanticKernel.SearchScenarioAsync(urlOllama, textModel: "llama3.2", embeddingModelName: "all-minilm", hostQdrant, collectionName: "movies");

        // Not working AI Extensions throwing Service not found
        //await KernelMemoryQdrantRag.RagWikipediaScenarioAsync(urlOllama, textModel: "phi3", embeddingModelName: "all-minilm", urlQdrant); //mxbai-embed-large

        // Not working AI Extensions throwing Service not found
        //await KernelMemoryQdrantRagSK.RagDocumentsScenarioAsync(urlOllama, textModel: "phi3", embeddingModelName: "all-minilm", urlQdrant, hostQdrant); //mxbai-embed-large
        //await KernelMemoryQdrantRagSK.ClinicScenarioAsync(urlOllama, textModel: "llama3.2", embeddingModelName: "all-minilm", urlQdrant, hostQdrant); //"calebfahlgren/natural-functions", mxbai-embed-large

        //https://learn.microsoft.com/en-us/semantic-kernel/how-to/vector-store-connectors/vector-store-data-ingestion
        //https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/TextChunkingAndEmbedding.cs
    }
}
