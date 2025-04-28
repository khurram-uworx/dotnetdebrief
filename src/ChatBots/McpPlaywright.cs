#pragma warning disable SKEXP0001

using ChatBots.Helpers;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBots;

class McpPlaywright
{
    Kernel kernel = null;

    public McpPlaywright(string urlOllama, string textModel)
    {
        this.kernel = SemanticKernelHelper.GetKernel(urlOllama, textModel);
    }

    public async Task HandleMcpPromptAsync(string prompt)
    {
        await using var mcpClient = await McpClientFactory.CreateAsync(
            new()
            {
                Id = "playwright",
                Name = "Playwright",
                TransportType = "stdio",
                TransportOptions = new Dictionary<string, string>
                {
                    ["command"] = "npx",
                    ["arguments"] = "-y @playwright/mcp@latest",
                }
            },
            new()
            {
                ClientInfo = new()
                {
                    Name = "Playwright", Version = "1.0.0"
                }
            }).ConfigureAwait(false);


        var tools = await mcpClient.EnumerateToolsAsync().ToListAsync();
        foreach (var tool in tools)
            Console.WriteLine($"{tool.Name}: {tool.Description}");

        this.kernel.Plugins.AddFromFunctions("playwright", tools.Select(
            aiFunction => aiFunction.AsKernelFunction()));

        prompt = $""""
            You are Browser assistant having access to Playwright functions. Using these functions / tools you can access
            Browser through Playwright and perform available functions to achieve what user is asking you using the browser.
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
