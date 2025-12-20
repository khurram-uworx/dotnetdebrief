using LLama.Abstractions;
using LLama.Common;
using Tools;

namespace Scenarios;

public static class ToolAgentScenario
{
    public static async Task RunAsync(ILLamaExecutor executor)
    {
        Console.WriteLine("\n[Tool-Using Agent]\n");
        string repo = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", ".."));
        var systemPrompt = $"""
        Use following tools to understand the code at path: {repo}

        - list_files(path)
        - read_file(path)

        What this project/repo is doing? Be succinct...
        """;

        var response = await RunLLMAsync(executor, systemPrompt);

        if (response.Contains("list_files"))
        {
            Console.WriteLine("\n[Tool] list_files(sample_project)\n");
            var files = FileTools.ListFiles("sample_project");
            Console.WriteLine(files);

            var followUp = systemPrompt + "\nFiles:\n" + files;
            response = await RunLLMAsync(executor, followUp);
        }

        Console.WriteLine("\nFinal Answer:\n" + response);
    }

    static async Task<string> RunLLMAsync(ILLamaExecutor executor, string prompt)
    {
        var inference = new InferenceParams
        {
            //Temperature = 0.3f,
            MaxTokens = 512
        };

        var result = "";
        await foreach (var token in executor.InferAsync(prompt, inference))
        {
            Console.Write(token);
            result += token;
        }

        Console.WriteLine();
        return result;
    }
}
