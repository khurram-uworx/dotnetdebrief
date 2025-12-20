using LLama.Abstractions;
using LLama.Common;

namespace Scenarios;

public static class ChatScenario
{
    public static async Task RunAsync(ILLamaExecutor executor)
    {
        Console.WriteLine("\n[Chat Scenario]\n");

        var prompt = """
        You are running locally on a Windows laptop.
        Explain what this program does in 3 sentences.
        """;

        await ExecuteAsync(executor, prompt);
    }

    static async Task ExecuteAsync(ILLamaExecutor executor, string prompt)
    {
        var inference = new InferenceParams
        {
            //Temperature = 0.7f,
            MaxTokens = 256
        };

        await foreach (var token in executor.InferAsync(prompt, inference))
            Console.Write(token);

        Console.WriteLine();
    }
}
