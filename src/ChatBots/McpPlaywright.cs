#pragma warning disable SKEXP0001

using ChatBots.Helpers;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBots;

//https://github.com/microsoft/playwright-mcp
//https://devblogs.microsoft.com/semantic-kernel/guest-blog-build-an-ai-app-that-can-browse-the-internet-using-microsofts-playwright-mcp-server-semantic-kernel-in-just-4-steps/
class McpPlaywright
{
    Kernel kernel = null;

    public McpPlaywright(string openApiUrl, string openApiKey, string openApiModel)
    {
        this.kernel = SemanticKernelHelper.GetKernel(openApiUrl, openApiKey, openApiModel);
    }

    public async Task HandleMcpPromptAsync(string prompt)
    {
        var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "Playwright",
            Command = "npx",
            Arguments = ["-y", "@playwright/mcp@latest"],
        });

        await using var mcpClient = await McpClient.CreateAsync(clientTransport)
            .ConfigureAwait(false);

        var tools = await mcpClient.ListToolsAsync();
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
