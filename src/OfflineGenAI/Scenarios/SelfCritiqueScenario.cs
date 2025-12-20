using LLama.Abstractions;
using LLama.Common;

namespace Scenarios;

public static class SelfCritiqueScenario
{
    public static async Task RunAsync(ILLamaExecutor executor)
    {
        Console.WriteLine("\n[Self-Critique Loop]\n");

        var problem = "Write a function that checks if a number is prime.";

        var solution = await RunAsync(executor, $"Solve this:\n{problem}");
        var critique = await RunAsync(executor, $"Critique this solution:\n{solution}");
        var improved = await RunAsync(executor, $"Improve the solution using the critique:\n{critique}");

        Console.WriteLine("\n--- Improved Solution ---\n");
        Console.WriteLine(improved);
    }

    static async Task<string> RunAsync(ILLamaExecutor executor, string prompt)
    {
        var inference = new InferenceParams
        {
            //Temperature = 0.5f,
            MaxTokens = 256
        };

        var output = "";
        await foreach (var token in executor.InferAsync(prompt, inference))
        {
            Console.Write(token);
            output += token;
        }

        Console.WriteLine("\n");
        return output;
    }
}
