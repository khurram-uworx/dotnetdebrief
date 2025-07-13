#pragma warning disable SKEXP0001

using ChatBots.Helpers;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBots;

class McpDatabase
{
    // https://devblogs.microsoft.com/azure-sql/introducing-mssql-mcp-server
    // https://github.com/Azure-Samples/SQL-AI-samples
    // https://devblogs.microsoft.com/azure-sql/mssql-mcp-server-in-action-susans-journey
    Kernel kernel = null;

    public McpDatabase(string openApiUrl, string openApiKey, string openApiModel)
    {
        this.kernel = SemanticKernelHelper.GetKernel(openApiUrl, openApiKey, openApiModel);
    }

    public async Task HandleMcpPromptAsync(string prompt)
    {
        var parentPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
        var path = Path.Combine(parentPath,
                "MssqlMcp", "bin", "Release", "net8.0", "MssqlMcp.exe");

        var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "Sql",
            Command = path,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                { "CONNECTION_STRING", "Server=.;Database=Northwind;Trusted_Connection=True;TrustServerCertificate=True" }
            }
        });

        await using var mcpClient = await McpClientFactory.CreateAsync(clientTransport)
            .ConfigureAwait(false);

        var tools = await mcpClient.EnumerateToolsAsync().ToListAsync();
        foreach (var tool in tools)
            Console.WriteLine($"{tool.Name}: {tool.Description}");

        this.kernel.Plugins.AddFromFunctions("sql", tools.Select(
            aiFunction => aiFunction.AsKernelFunction()));

        prompt = $""""
            Instructions:
            You are database assistant having access to Database functions. Use these available functions to perform what user is asking you.
            When you need, discover the schema using these functions.
            When calling these functions, be mindful about their names and needed arguments. Use appropriate names and data types for a
            neat function call.

            User Prompt:
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
