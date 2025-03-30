#pragma warning disable SKEXP0001

using ChatBots.Helpers;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
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

    public McpRepository(string urlOllama, string textModel)
    {
        this.kernel = SemanticKernelHelper.GetKernel(urlOllama, textModel);
    }

    public async Task HandleMcpPromptAsync(string prompt)
    {
        // Github is one example; https://github.com/modelcontextprotocol/servers for many more
        // Create an MCPClient for the GitHub server
        await using var mcpClient = await McpClientFactory.CreateAsync(
            new()
            {
                Id = "github",
                Name = "GitHub",
                TransportType = "stdio",
                TransportOptions = new Dictionary<string, string>
                {
                    ["command"] = "npx", // yes our MCP Server is running on Node 
                    ["arguments"] = "-y @modelcontextprotocol/server-github",
                }
            },
            new()
            {
                ClientInfo = new()
                {
                    Name = "GitHub", Version = "1.0.0"
                }
            }).ConfigureAwait(false);


        // Retrieve the list of tools available on the GitHub server
        var tools = await mcpClient.EnumerateToolsAsync().ToListAsync();
        // hydrated so we can use Select later; seeing SelectAsync still hurt
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
