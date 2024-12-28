using System.Threading.Tasks;

namespace ChatBots.KM;

internal static class Program
{
    static async Task Main(string[] args)
    {
        //docker run --rm -p 6333:6333 -p 6334:6334 qdrant/qdrant

        await KernelMemoryQdrantRag.RagWikipediaScenarioAsync(textModel: "phi3", embeddingModelName: "mxbai-embed-large");

        // Not working AI Extensions throwing Service not found
        // await KernelMemoryQdrantRagSK.RagDocumentsScenarioAsync(textModel: "phi3", embeddingModelName: "mxbai-embed-large");
        //await KernelMemoryQdrantRagSK.ClinicScenarioAsync(textModel: "llama3.2", embeddingModelName: "mxbai-embed-large"); //"calebfahlgren/natural-functions"
    }
}
