#pragma warning disable MAAI001

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System;
using System.Threading.Tasks;

namespace ChatBots.AgentFramework;

class HarnessAgents
{
    //https://devblogs.microsoft.com/agent-framework/the-microsoft-agent-framework-harness-is-now-released/
    //https://devblogs.microsoft.com/agent-framework/build-your-own-claw-and-agent-harness-with-microsoft-agent-framework/
    //https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/

    public static async Task RunHarnessAsync(IChatClient chatClient)
    {
        //https://learn.microsoft.com/en-us/agent-framework/agents/harness
        AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
        {
            LoopEvaluators = [new CompletionMarkerLoopEvaluator("DONE")],
        });

        //AgentResponse response = await agent.RunAsync("Plan a weekend trip to Lahore.");
        //Console.WriteLine(response.Text);

        var instructions = """
            Help me plan a 14-day trip to Japan.

            Budget: $2500
            I like food, hiking, and anime.
            Avoid rushing.

            Keep a todo list of planning tasks.

            Whenever I change a requirement, update the plan instead of starting over.
            """;

        await foreach (var r in agent.RunStreamingAsync(instructions))
            Console.Write(r.Text);
    }

    public static async Task RunHarnessAsync(string urlOllama, string model)
    {
        IChatClient chatClient = new OllamaApiClient(new Uri(urlOllama), model);
        await RunHarnessAsync(chatClient);
    }
}
