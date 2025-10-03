#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110

using ChatBots.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBots;

class SkAgentCreativeWriter
{
    //https://github.com/Azure-Samples/aspire-semantic-kernel-creative-writer

    //const string ResearcherName = "Researcher";
    //const string MarketingName = "Marketing";
    //const string WriterName = "Writer";
    const string EditorName = "Editor";

    class NoFeedbackLeftTerminationStrategy : TerminationStrategy
    {
        // Terminate when the final message contains the term "Article accepted, no further rework necessary." - all done
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        {
            if (agent.Name != EditorName)
                return Task.FromResult(false);

            return Task.FromResult(history[history.Count - 1].Content?.Contains("Article accepted", StringComparison.OrdinalIgnoreCase) ?? false);
        }
    }

    Kernel kernel = null;

    public SkAgentCreativeWriter(string urlOllama, string textModel)
    {
        this.kernel = SemanticKernelHelper.GetKernel(urlOllama, textModel);
    }

    public async Task WriteCreativelyAsync(string prompt)
    {
        Console.WriteLine(prompt);

        Func<string, ChatCompletionAgent> agentSetup = file =>
        {
            var agentPrompt = File.ReadAllText(file);
            var prompt = KernelFunctionYaml.ToPromptTemplateConfig(agentPrompt);
            return new ChatCompletionAgent(prompt, new KernelPromptTemplateFactory()) { Kernel = this.kernel };
        };

        var editor = agentSetup("WriterAgents/editor.yaml");
        var marketing = agentSetup("WriterAgents/marketing.yaml");
        var researcher = agentSetup("WriterAgents/researcher.yaml");
        var writer = agentSetup("WriterAgents/writer.yaml");

        try
        {
            var chat = new AgentGroupChat(writer, editor)
            {
                ExecutionSettings = new()
                {
                    TerminationStrategy = new NoFeedbackLeftTerminationStrategy(),
                    SelectionStrategy = new SequentialSelectionStrategy() { InitialAgent = writer },
                }
            };

            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, prompt));

            await foreach (var content in chat.InvokeAsync())
            {
                Console.WriteLine();
                string color = content.AuthorName switch
                {
                    "Editor" => "\u001b[34m", // Blue
                    "Marketing" => "\u001b[32m", // Green
                    "Researcher" => "\u001b[35m", // Magenta
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
