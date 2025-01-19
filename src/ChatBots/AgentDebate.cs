#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110

using ChatBots.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ChatBots;

class AgentDebate
{
    //https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-chat
    //https://github.com/microsoft/all-things-azure/blob/main/agentic-philosophers
    //const string PlatoFileName = "Plato.pdf";
    const string SocratesName = "Socrates";
    const string PlatoName = "Plato";
    const string AristotleName = "Aristotle";

    public async Task DebateAsync(string urlOllama, string textModel, string prompt)
    {
        Console.WriteLine(prompt);

        var kernel = SemanticKernelHelper.GetKernel(urlOllama, textModel,
            (b, c) =>
            {
            });

        Func<string, ChatCompletionAgent> agentSetup = file =>
        {
            var agentPrompt = File.ReadAllText(file);
            var prompt = KernelFunctionYaml.ToPromptTemplateConfig(agentPrompt);
            return new ChatCompletionAgent(prompt) { Kernel = kernel };
        };

        var socrates = agentSetup("PromptTemplates/SocratesAgent.yaml");
        var aristotle = agentSetup("PromptTemplates/AristotleAgent.yaml");
        var plato = agentSetup("PromptTemplates/PlatoAgent.yaml");

        try
        {
            var terminateFunction = KernelFunctionFactory.CreateFromPrompt(
                $$$"""
                    History:
                    {{$history}}
                    
                    VERY VERY Important: Make sure every participant gets a chance to speak; these are the participants
                    - {{{SocratesName}}}
                    - {{{PlatoName}}}
                    - {{{AristotleName}}}
                    """
                );
            var selectionFunction = KernelFunctionFactory.CreateFromPrompt(
                $$$"""
                    History:
                    {{$history}}

                    Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
                    State only the name of the participant to take the next turn.

                    Always follow these steps when selecting the next participant:
                    1) After user input, it is {{{SocratesName}}}'s turn to respond.
                    2) After {{{SocratesName}}} replies, it's {{{PlatoName}}}'s turn based on {{{SocratesName}}}'s response.
                    3) After {{{PlatoName}}} replies, it's {{{AristotleName}}}'s turn based on {{{SocratesName}}}'s response.
                    4) After {{{AristotleName}}} replies, it's {{{SocratesName}}}'s turn to summarize the responses and end the conversation.

                    Make sure each participant has a turn

                    VERY VERY Important: Choose only from these participants and in your reply; only state their name; dont describe anything else:
                    - {{{SocratesName}}}
                    - {{{PlatoName}}}
                    - {{{AristotleName}}}
                    """
            );
            var chat = new AgentGroupChat(socrates, aristotle, plato)
            {
                ExecutionSettings = new()
                {
                    TerminationStrategy = new KernelFunctionTerminationStrategy(terminateFunction, kernel)
                    {
                        Agents = [socrates],
                        ResultParser = (result) =>
                            result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                        HistoryVariableName = "history",
                        MaximumIterations = 4
                    },
                    SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, kernel)
                    {
                        AgentsVariableName = "agents",
                        HistoryVariableName = "history",
                        ResultParser = result =>
                            (result.GetValue<string>() ?? "").Trim().TrimEnd('.')
                    }
                }
            };

            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, prompt));

            await foreach (var content in chat.InvokeAsync())
            {
                Console.WriteLine();
                string color = content.AuthorName switch
                {
                    "Socrates" => "\u001b[34m", // Blue
                    "Aristotle" => "\u001b[32m", // Green
                    "Plato" => "\u001b[35m", // Magenta
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
