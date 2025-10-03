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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBots;

class SkAgentDebate
{
    //https://github.com/microsoft/all-things-azure/blob/main/agentic-philosophers
    //const string PlatoFileName = "Plato.pdf";
    const string SocratesName = "Socrates";
    const string PlatoName = "Plato";
    const string AristotleName = "Aristotle";
    Kernel kernel = null;

    class AgentDebateSelectionStrategy : SelectionStrategy
    {
        Agent socrates, aristotle, plato;

        public AgentDebateSelectionStrategy(Agent socrates, Agent aristotle, Agent plato) =>
            (this.socrates, this.aristotle, this.plato) = (socrates, aristotle, plato);

        protected override Task<Agent> SelectAgentAsync(IReadOnlyList<Agent> agents, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
        {
            if (history.Count <= 1)
                return Task.FromResult(socrates);
            else
            {
                var lastChat = history.Last();

                if (lastChat.Content is not null && lastChat.Content.ToString().Trim() == string.Empty)
                {
                    if (lastChat.AuthorName == socrates.Name)
                        return Task.FromResult(socrates);
                    else if (lastChat.AuthorName == plato.Name)
                        return Task.FromResult(plato);
                    else if (lastChat.AuthorName == aristotle.Name)
                        return Task.FromResult(aristotle);
                }

                if (lastChat.AuthorName == socrates.Name)
                    return Task.FromResult(plato);
                else if (lastChat.AuthorName == plato.Name)
                    return Task.FromResult(aristotle);
                else if (lastChat.AuthorName == aristotle.Name)
                    return Task.FromResult(socrates);

                return Task.FromResult(socrates);
            }
        }
    }

    public SkAgentDebate(string urlOllama, string textModel)
    {
        this.kernel = SemanticKernelHelper.GetKernel(urlOllama, textModel);
    }

    public async Task DebateAsync(string prompt)
    {
        Console.WriteLine(prompt);

        Func<string, ChatCompletionAgent> agentSetup = file =>
        {
            var agentPrompt = File.ReadAllText(file);
            var prompt = KernelFunctionYaml.ToPromptTemplateConfig(agentPrompt);
            return new ChatCompletionAgent(prompt, new KernelPromptTemplateFactory()) { Kernel = this.kernel };
        };

        // https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-templates
        // https://learn.microsoft.com/en-us/semantic-kernel/concepts/prompts/yaml-schema

        var socrates = agentSetup("DebateAgents/SocratesAgent.yaml");
        var aristotle = agentSetup("DebateAgents/AristotleAgent.yaml");
        var plato = agentSetup("DebateAgents/PlatoAgent.yaml");

        try
        {
            var terminateFunction = KernelFunctionFactory.CreateFromPrompt(
                // if we dont provide history ai will not be able to determine due to no context
                $$$"""
                    Your job is to determine if discussion is completed.

                    Here's the history of the discussion so far
                    {{$history}}
                    
                    You will decide using two things; first make sure every participant gets a chance to speak; these are the participants
                    - {{{SocratesName}}}
                    - {{{PlatoName}}}
                    - {{{AristotleName}}}
                    and second assess if all the questions or concerns of {{{SocratesName}}} are addressed or {{{SocratesName}}} has nothing else to say
                    or {{{SocratesName}}} is looking satisfied with the debate

                    Important: only give yes or no answer; dont describe anything else. Say yes if discussion is completed and all have participated; otherwise say no
                    """
                );
            var selectionFunction = KernelFunctionFactory.CreateFromPrompt(
                $$$"""
                    Your job is to determine which participant takes the next turn in the debate.

                    Choose only from these participants
                    - {{{SocratesName}}}
                    - {{{PlatoName}}}
                    - {{{AristotleName}}}
                    
                    Always follow these steps when selecting the next participant:
                    - After user it is always {{{SocratesName}}} turn
                    - After {{{SocratesName}}} replies, its {{{PlatoName}}} turn
                    - After {{{PlatoName}}} replies, it's {{{AristotleName}}} turn
                    - After {{{AristotleName}}} replies, its {{{SocratesName}}} turn
                                        
                    Here's the history of the discussion so far
                    {{$history}}
                                                            
                    VERY Important: In your reply; only state their name; dont describe anything else and DO NOT select the participant again who has just responded
                    and is the last in the history above
                    """
            );
            var chat = new AgentGroupChat(socrates, aristotle, plato)
            {
                ExecutionSettings = new()
                {
                    TerminationStrategy = new KernelFunctionTerminationStrategy(terminateFunction, this.kernel)
                    {
                        Agents = [socrates],
                        ResultParser = (result) =>
                            result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                        HistoryVariableName = "history",
                        //MaximumIterations = 10
                    },
                    //SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, kernel)
                    //{
                    //    AgentsVariableName = "agents",
                    //    HistoryVariableName = "history",
                    //    ResultParser = result =>
                    //        (result.GetValue<string>() ?? "").Trim().TrimEnd('.')
                    //}
                    SelectionStrategy = new AgentDebateSelectionStrategy(socrates, aristotle, plato)
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
