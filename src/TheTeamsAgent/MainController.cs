#pragma warning disable SKEXP0001

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

using ModelContextProtocol.Client;

namespace TheTeamsAgent;

[TeamsController("main")]
public class MainController
{
    bool initialized = false;
    Kernel kernel = null;

    public MainController(Kernel kernel)
    {
        this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    async Task<Kernel> getMcpConnectedKernelAsync()
    {
        if (this.initialized) return kernel;

        var parentPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));
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

        var mcpClient = McpClientFactory.CreateAsync(clientTransport).Result;
                var toolsList = new List<McpClientTool>();
        await foreach (var tool in mcpClient.EnumerateToolsAsync())
        {
            Console.WriteLine($"{tool.Name}: {tool.Description}");
            toolsList.Add(tool);
        }

        this.kernel.Plugins.AddFromFunctions("sql", toolsList.Select(
            aiFunction => aiFunction.AsKernelFunction()));

        this.initialized = true;
        return kernel;
    }

    [Message]
    public async Task OnMessage([Context] MessageActivity activity, [Context] IContext.Client client)
    {
        //await client.Send($"you said \"{activity.Text}\"");

        ChatHistory chatHistory;
        // Get chatHistory from the activity's Properties dictionary
        if (!activity.Properties.TryGetValue("conversation.chatHistory", out var chatHistoryObj) || chatHistoryObj is not ChatHistory)
        {
            chatHistory = new ChatHistory();
            activity.Properties["conversation.chatHistory"] = chatHistory;
        }
        else
            chatHistory = (ChatHistory)chatHistoryObj;

        ChatMessageContent message = new(AuthorRole.User, activity.Text);
        chatHistory.Add(message);

        var k = await this.getMcpConnectedKernelAsync();
        var chatCompletionService = k.Services.GetRequiredService<IChatCompletionService>();
        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var result = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: openAIPromptExecutionSettings,
                kernel: k);
        if (!string.IsNullOrWhiteSpace(result.Content))
        {
            chatHistory.AddMessage(result.Role, result.Content);

            activity.Properties["conversation.chatHistory"] = chatHistory;
            await client.Send(result.Content);
        }
    }
}