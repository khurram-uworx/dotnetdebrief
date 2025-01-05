using System.Threading.Tasks;

namespace ChatBots.KM;

internal static class Program
{
    static async Task Main(string[] args)
    {
        var urlOllama = "http://localhost:11434";

        //docker run --rm -p 6333:6333 -p 6334:6334 qdrant/qdrant
        var urlQdrant = "http://localhost:6333";
        var hostQdrant = "localhost";

        await KernelMemoryQdrantRag.RagWikipediaScenarioAsync(urlOllama, textModel: "llama3.2", embeddingModelName: "all-minilm", urlQdrant);

        // Not working AI Extensions throwing Service not found
        //await KernelMemoryQdrantRagSK.RagDocumentsScenarioAsync(urlOllama, textModel: "llama3.2", embeddingModelName: "all-minilm", urlQdrant, hostQdrant);
        //await KernelMemoryQdrantRagSK.ClinicScenarioAsync(urlOllama, textModel: "llama3.2", embeddingModelName: "all-minilm", urlQdrant, hostQdrant); //"calebfahlgren/natural-functions"
    }
}
