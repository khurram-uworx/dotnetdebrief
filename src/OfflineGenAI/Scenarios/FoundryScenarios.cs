using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Microsoft.AI.Foundry.Local;

namespace Scenarios;

class FoundryScenarios
{
    //https://github.com/intel/Microsoft-Build2025-Samples/blob/main/FoundryLocalApp/Program.cs

    CancellationToken ct = new();

    public async Task RunAsync()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var config = new Configuration
        {
            AppName = "dotnetdebrief",
            //ModelCacheDir = Path.Combine(userProfile, ".foundry", "cache", "models"),
            LogLevel = LogLevel.Information
        };

        // Initialize the singleton instance.
        await FoundryLocalManager.CreateAsync(config, Utils.GetAppLogger());
        var mgr = FoundryLocalManager.Instance;

        // Ensure that any Execution Provider (EP) downloads run and are completed.
        // EP packages include dependencies and may be large.
        // Download is only required again if a new version of the EP is released.
        // For cross platform builds there is no dynamic EP download and this will return immediately.
        await Utils.RunWithSpinner("Registering execution providers", mgr.EnsureEpsDownloadedAsync());

        var catalog = await mgr.GetCatalogAsync();
        var model = await catalog.GetModelAsync("qwen2.5-0.5b") ?? throw new Exception("Model not found");

        await model.DownloadAsync(progress =>
        {
            Console.Write($"\rDownloading model: {progress:F2}%");
            if (progress >= 100f)
            {
                Console.WriteLine();
            }
        });

        Console.Write($"Loading model {model.Id}...");
        await model.LoadAsync();
        Console.WriteLine("done.");

        var chatClient = await model.GetChatClientAsync();

        List<ChatMessage> messages = new()
        {
            new ChatMessage { Role = "user", Content = "Why is the sky blue?" }
        };

        Console.WriteLine("Chat completion response:");
        var streamingResponse = chatClient.CompleteChatStreamingAsync(messages, ct);
        await foreach (var chunk in streamingResponse)
        {
            Console.Write(chunk.Choices[0].Message.Content);
            Console.Out.Flush();
        }
        Console.WriteLine();

        // Tidy up - unload the model
        await model.UnloadAsync();
    }
}
