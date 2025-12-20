using LLama.Abstractions;
using LLama.Common;

namespace Scenarios;

public static class CodeReviewScenario
{
    public static async Task RunAsync(ILLamaExecutor executor)
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

        await ExecuteAsync(executor, prompt);
    }

    static async Task ExecuteAsync(ILLamaExecutor executor, string prompt)
    {
        var inference = new InferenceParams
        {
            //Temperature = 0.4f,
            MaxTokens = 256
        };

        await foreach (var token in executor.InferAsync(prompt, inference))
            Console.Write(token);

        Console.WriteLine();
    }
}
