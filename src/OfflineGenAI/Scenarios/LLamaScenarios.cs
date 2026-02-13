using LLama.Abstractions;
using LLama.Common;

namespace Scenarios;

public static class LLamaScenarios
{
    static async Task<string> executeAsync(ILLamaExecutor executor, string prompt)
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

        Console.WriteLine();
        return output;
    }

    public static async Task PromptScenarioAsync(ILLamaExecutor executor)
    {
        Console.WriteLine("\n[Prompt Scenario]\n");

        var prompt = """
        You are running locally on a Windows laptop.
        Explain what this program does in 3 sentences.
        """;

        await executeAsync(executor, prompt);
    }

    public static async Task CodeReviewScenarioAsync(ILLamaExecutor executor)
    {
        Console.WriteLine("\n[Code Review]\n");

        var code = """
        public int Divide(int a, int b)
        {
            return a / b;
        }
        """;

        var prompt = $"""
        Review this code:
        {code}

        Identify bugs and suggest improvements.
        """;

        await executeAsync(executor, prompt);
    }

    public static async Task SelfCritiqueScenarioAsync(ILLamaExecutor executor)
    {
        Console.WriteLine("\n[Self Critique]\n");

        var problem = "Write a function that checks if a number is prime.";

        var solution = await executeAsync(executor, $"Solve this:\n{problem}");
        var critique = await executeAsync(executor, $"Critique this solution:\n{solution}");
        var improved = await executeAsync(executor, $"Improve the solution using the critique:\n{critique}");

        Console.WriteLine("\n--- Improved Solution ---\n");
        Console.WriteLine(improved);
    }
}
