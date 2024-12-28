using System.Threading.Tasks;

namespace ChatBots;

internal static class Program
{
    static async Task Main(string[] args)
    {
        //OpenAI.Run(textModel: "phi3:mini");
        //ML.MLTest(textModel: "directml-int4-awq-block-128");

        //OpenAITools.ChatWithTools(textModel: "mistral");

        //await AutoGenChats.HelloOllamaWorldAsync(textModel: "phi3:latest");
        //await AutoGenChats.HelloAgents(textModel: "mistral");
        // https://microsoft.github.io/autogen-for-net/articles/Built-in-messages.html

        // llama2 and phi3 dont support tools
        //await SemanticKernelChats.HelloWorldAsync(textModel: "mistral");
        //await SemanticKernelChats.PromptScenarioAsync(textModel: "mistral");
        //await SemanticKernelChats.LightsPluginAsync(textModel: "mistral");    // User: Please toggle all the lights
        //await SemanticKernelChats.RagScenarioAsync(textModel: "mistral");     // not working because of Semantic kernel with ollama text embedding search does not return any value
        // https://github.com/microsoft/semantic-kernel/issues/6483

        await AIExtensionTools.ChatWithAIExtensionAsync(textModel: "llama3.2");

        //docker run --rm -p 6333:6333 -p 6334:6334 qdrant/qdrant
        //await Qdrant.QuickStartAsync(collectionName: "test_collection");
        //await Qdrant.VectorDataExtensionsAsync(embeddingModelName: "all-minilm", collectionName: "movies");

        //await QdrantSemanticKernel.RagClinicScenarioAsync(textModel: "llama3.2", memoryCollectionName: "timingFacts");
        //await QdrantSemanticKernel.SearchScenarioAsync(textModel: "llama3.2", embeddingModelName: "all-minilm", collectionName: "movies");

        // Not working AI Extensions throwing Service not found
        //await KernelMemoryQdrantRag.RagWikipediaScenarioAsync(textModel: "phi3", embeddingModelName: "mxbai-embed-large");

        // Not working AI Extensions throwing Service not found
        //await KernelMemoryQdrantRagSK.RagDocumentsScenarioAsync(textModel: "phi3", embeddingModelName: "mxbai-embed-large");
        //await KernelMemoryQdrantRagSK.ClinicScenarioAsync(textModel: "llama3.2", embeddingModelName: "mxbai-embed-large"); //"calebfahlgren/natural-functions"

        //https://learn.microsoft.com/en-us/semantic-kernel/how-to/vector-store-connectors/vector-store-data-ingestion
        //https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/TextChunkingAndEmbedding.cs
    }
}
