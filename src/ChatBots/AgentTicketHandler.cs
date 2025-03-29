#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110

using ChatBots.Helpers;
using ChatBots.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBots;

class AgentTicketHandler
{
    //https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-chat
    
    //https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/GettingStartedWithAgents/Step02_Plugins.cs

    //Updates : https://devblogs.microsoft.com/semantic-kernel/semantic-kernel-agent-framework-rc2/
    //          https://devblogs.microsoft.com/semantic-kernel/release-the-agents-sk-agents-framework-rc1/

    class TicketHandlerTerminationStrategy : TerminationStrategy
    {
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        {
            if (agent.Name == "RecommendationGenerator")
                return Task.FromResult(true);
            else
                return Task.FromResult(false);
        }
    }

    class TicketHandlerSelectionStrategy : SelectionStrategy
    {
        protected override Task<Agent> SelectAgentAsync(IReadOnlyList<Agent> agents, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
        {
            if (history.Count <= 1)
                return Task.FromResult(agents.First(a => a.Name == "TicketDetailsExtractor"));

            var lastChat = history.Last();

            if (lastChat is not null)
            {
                if (string.IsNullOrEmpty(lastChat.Content))
                    return Task.FromResult(agents.First(a => a.Name == lastChat.AuthorName));
                else if (lastChat.AuthorName == "TicketDetailsExtractor")
                    return Task.FromResult(agents.First(a => a.Name == "TicketCategorizer"));
                else if (lastChat.AuthorName == "TicketCategorizer")
                    return Task.FromResult(agents.First(a => a.Name == "PriorityAssessor"));
                else if (lastChat.AuthorName == "PriorityAssessor")
                    return Task.FromResult(agents.First(a => a.Name == "RecommendationGenerator"));
            }

            return Task.FromResult(agents.First(a => a.Name == "TicketDetailsExtractor"));
        }
    }

    Kernel kernel = null;

    public AgentTicketHandler(string urlOllama, string textModel)
    {
        this.kernel = SemanticKernelHelper.GetKernel(urlOllama, textModel);
    }

    public async Task HandleTicketAsync(string prompt)
    {
        Console.WriteLine(prompt);

        Func<Kernel, string, KernelArguments, ChatCompletionAgent> agentArgumentSetup = (k, file, arguments) =>
        {
            var agentPrompt = File.ReadAllText(file);
            var prompt = KernelFunctionYaml.ToPromptTemplateConfig(agentPrompt);

            if (arguments != null)
                return new ChatCompletionAgent(prompt, new KernelPromptTemplateFactory()) { Kernel = k, Arguments = arguments };
            else
                return new ChatCompletionAgent(prompt, new KernelPromptTemplateFactory()) { Kernel = k };
        };
        Func<Kernel, string, ChatCompletionAgent> agentSetup = (k, file) =>
            agentArgumentSetup(k, file, null);

        var clonedKernel = this.kernel.Clone();
        clonedKernel.Plugins.AddFromType<TicketsPlugin>("Tickets");

        var extractor = agentSetup(this.kernel, "TicketAgents/1-TicketDetailsExtractor.yaml");
        var categorizer = agentArgumentSetup(clonedKernel, "TicketAgents/2-TicketCategorizer.yaml",
            new KernelArguments(
                new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Required()
                }
            ));
        var priority = agentSetup(this.kernel, "TicketAgents/3-PriorityAssessor.yaml");
        var recommendation = agentSetup(this.kernel, "TicketAgents/4-RecommendationGenerator.yaml");

        try
        {
            var chat = new AgentGroupChat(extractor, categorizer, priority, recommendation)
            {
                ExecutionSettings = new()
                {
                    TerminationStrategy = new TicketHandlerTerminationStrategy(),
                    SelectionStrategy = new TicketHandlerSelectionStrategy() //new SequentialSelectionStrategy() { InitialAgent = extractor }
                }
            };

            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, prompt));

            await foreach (var content in chat.InvokeAsync())
            {
                Console.WriteLine();
                string color = content.AuthorName switch
                {
                    // if we want to print different agent messages in different colors
                    _ => "\u001b[0m" // Default color
                };
                Console.WriteLine($"{color}[{content.AuthorName ?? "*"}]: '{content.Content}'\u001b[0m");
                Console.WriteLine();
            }
        }
        finally
        {
            // Cleanup thread/vector/file stores
        }
    }
}
