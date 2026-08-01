using AgentFramework;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System;
using System.Threading.Tasks;

namespace ChatBots.AgentFramework;

class Agents
{
    public static async Task AgentWorkflowExecutorAsync(string urlOllama, string model)
    {
        //https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/GettingStarted/Workflows/Agents/CustomAgentExecutors/Program.cs
        IChatClient chatClient = new OllamaApiClient(new Uri(urlOllama), model);

        var sloganWriter = new SloganWriterExecutor("SloganWriter", chatClient);
        var feedbackProvider = new FeedbackExecutor("FeedbackProvider", chatClient);

        var workflow = new WorkflowBuilder(sloganWriter)

            .AddEdge(sloganWriter, feedbackProvider)
            .AddEdge(feedbackProvider, sloganWriter)

            .WithOutputFrom(feedbackProvider)
            .Build();

        StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow,
            input: "Create a slogan for a new electric SUV that is affordable and " +
            "fun to drive.");

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is SloganGeneratedEvent or FeedbackEvent) // Custom events to allow us to monitor the progress of the workflow.
                Console.WriteLine($"{evt}");

            if (evt is WorkflowOutputEvent outputEvent)
                Console.WriteLine($"{outputEvent}");
        }
    }
}
