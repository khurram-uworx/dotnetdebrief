#pragma warning disable SKEXP0001

using System;
using System.Collections.Generic;
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
using ModelContextProtocol.Protocol;

using Tools;

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

        var clientTransport = new StreamClientTransport(EchoTool.ClientOutput, EchoTool.ClientInput);
        var mcpClient = await McpClient.CreateAsync(clientTransport);
        var tools = await mcpClient.ListToolsAsync();

        foreach (var tool in tools)
            Console.WriteLine($"{tool.Name}: {tool.Description}");

        this.kernel.Plugins.AddFromFunctions("sql", tools.Select(
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
            chatHistory.Add(new ChatMessageContent(AuthorRole.System,
                """
                You are a helpful reporting assistant. You can answer questions by suggesting the reports and can generate the selected report using the sql tool

                The system has the following reports available:
                - Report1: The velocity of a specified team
                - Report2: The current sprint status for a specified team
                - Report3: Summarize the progress of a specified team

                We have following teams:
                - Alpha
                - Bravo
                - Charlie

                IMPORTANT
                - Always use the name of the report as it is returned by the system, do not change it.
                - Always use the team name from the list above, do not change it.
                - Always use the report url as it is returned by the system, do not change it.

                """));

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