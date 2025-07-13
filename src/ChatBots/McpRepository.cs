#pragma warning disable SKEXP0001

using ChatBots.Helpers;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBots;

class McpRepository
{
    // https://www.anthropic.com/news/model-context-protocol
    // https://github.com/modelcontextprotocol
    //      https://github.com/modelcontextprotocol/csharp-sdk

    // https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Demos/ModelContextProtocolPlugin/Program.cs
    //      this is this example

    // Updates: https://devblogs.microsoft.com/semantic-kernel/integrating-model-context-protocol-tools-with-semantic-kernel-a-step-by-step-guide/
    //          https://devblogs.microsoft.com/semantic-kernel/building-a-model-context-protocol-server-with-semantic-kernel/

    // Others:  https://github.com/StefH/McpDotNet.Extensions.SemanticKernel

    Kernel kernel = null;

    public McpRepository(string openApiUrl, string openApiKey, string openApiModel)
    {
        this.kernel = SemanticKernelHelper.GetKernel(openApiUrl, openApiKey, openApiModel);
    }

    public async Task HandleMcpPromptAsync(string prompt)
    {
        // Github is one example; https://github.com/modelcontextprotocol/servers for many more
        var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "GitHub",
            Command = "npx",
            Arguments = ["-y", "@modelcontextprotocol/server-github"],
        });

        await using var mcpClient = await McpClientFactory.CreateAsync(clientTransport)
            .ConfigureAwait(false);

        // Retrieve the list of tools available on the GitHub server
        var tools = await mcpClient.EnumerateToolsAsync().ToListAsync();
        foreach (var tool in tools)
            Console.WriteLine($"{tool.Name}: {tool.Description}");

        this.kernel.Plugins.AddFromFunctions("github", tools.Select(
            aiFunction => aiFunction.AsKernelFunction()));

        // frontier models like gpt/claude can work seamlessly
        // our Ollama SLMs might struggle to infer MCP commands and at times we have to guide it
        // its similar how you have to guide interns / juniors at every step
        prompt = $""""
            You are Github assistant having access to Github functions. Use these available functions to perform what user is asking you.
            When calling these functions, be mindful about their names and needed arguments. Use appropriate names and data types for a
            neat function call.

            {prompt}
            """";

        var result = await kernel.InvokePromptAsync(prompt,
            new KernelArguments(new PromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Required()
            }));
        
        Console.WriteLine($"\n\n{prompt}\n{result}");
    }
}
