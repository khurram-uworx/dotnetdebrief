using System.Threading.Tasks;

namespace ChatBots;

internal static class Program
{
    static async Task Main(string[] args)
    {
        //OpenAI.Run();
        //ML.MLTest();

        //OpenAITools.ChatWithTools();

        //await AutoGen.HelloWorldPhi3Async();
        //await AutoGenChats.HelloAgents();
        // https://microsoft.github.io/autogen-for-net/articles/Built-in-messages.html

        //await SemanticKernelChats.HelloWorldAsync();
        //await SemanticKernelChats.PromptScenarioAsync();
        //await SemanticKernelChats.LightsPluginAsync();    // User: Please toggle all the lights
        //await SemanticKernelChats.RagScenarioAsync();     // not working because of Semantic kernel with ollama text embedding search does not return any value
        // https://github.com/microsoft/semantic-kernel/issues/6483

        //docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
        await Qdrant.QuickStartAsync();
        //await Qdrant.VectorDataExtensionsAsync();
        //await QdrantSemanticKernel.RagClinicScenarioAsync(memoryCollectionName: "timingFacts");

        //await KernelMemoryQdrantRag.RagWikipediaScenarioAsync();
        //await KernelMemoryQdrantRagSK.RagDocumentsScenarioAsync();
        //await KernelMemoryQdrantRagSK.ClinicScenarioAsync();

        //https://learn.microsoft.com/en-us/semantic-kernel/how-to/vector-store-connectors/vector-store-data-ingestion?pivots=programming-language-csharp
        //https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/TextChunkingAndEmbedding.cs
    }
}
