using AgentFramework;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBots;

class Agents
{
    //https://devblogs.microsoft.com/dotnet/introducing-microsoft-agent-framework-preview/

    [Description("Gets the author of the story.")]
    static string GetAuthor() => """
        The Brothers Grimm, Jacob (1785–1863) and Wilhelm (1786–1859)
        - They were German academics who together collected and published folklore.
        - The brothers are among the best-known storytellers of folktales
        """.Trim();

    [Description("Formats the story for display.")]
    static string FormatStory(string title, string author, string story) =>
        $"Title: {title}\nAuthor: {author}\n\n{story}";

    static int getIntegerFromHuman(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            
            string? input = Console.ReadLine();
            if (int.TryParse(input, out int value))
                return value;
            
            Console.WriteLine("Invalid input. Please enter a valid integer.");
        }
    }
    
    static ExternalResponse handleExternalRequest(ExternalRequest request)
    {
        var signal = request.DataAs<SignalWithNumber>();

        if (signal is not null)
        {
            switch (signal.Signal)
            {
                case NumberSignal.Init:
                    int initialGuess = getIntegerFromHuman("Please provide your initial guess: ");
                    return request.CreateResponse(initialGuess);
                case NumberSignal.Above:
                    int lowerGuess = getIntegerFromHuman($"You previously guessed {signal.Number} too large. Please provide a new guess: ");
                    return request.CreateResponse(lowerGuess);
                case NumberSignal.Below:
                    int higherGuess = getIntegerFromHuman($"You previously guessed {signal.Number} too small. Please provide a new guess: ");
                    return request.CreateResponse(higherGuess);
            }
        }

        throw new NotSupportedException($"Request {request.PortInfo.RequestType} is not supported");
    }

    public static async Task WriterEditorAsync(string urlOllama, string model)
    {
        //IChatClient chatClient = new ChatClient("gpt-4o-mini",
        //    new ApiKeyCredential(Environment.GetEnvironmentVariable("GITHUB_TOKEN")!),
        //    new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") })
        //    .AsIChatClient();
        IChatClient chatClient = new OllamaApiClient(new Uri(urlOllama), model);
        var prompt = "Write a short story about Hansel and Gretel.";

        AIAgent writer = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "Writer",
                ChatOptions = new ChatOptions
                {
                    Instructions = "Write stories that are engaging and creative.",
                    Tools = [
                        AIFunctionFactory.Create(GetAuthor),
                        AIFunctionFactory.Create(FormatStory)
                    ]
                }
            });
        
        AgentRunResponse response = await writer.RunAsync(prompt);
        Console.WriteLine("Single Agent Response:");
        Console.WriteLine(response.Text);

        AIAgent editor = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "Editor",
                ChatOptions = new ChatOptions
                {
                    Instructions = "Make the story more engaging, fix grammar, and enhance the plot."
                }
            });

        Workflow workflow = AgentWorkflowBuilder.BuildSequential([writer, editor]);
        AIAgent workflowAgent = workflow.AsAgent();
        AgentRunResponse workflowResponse = await workflowAgent.RunAsync(prompt);

        Console.WriteLine("\n\n\nWorkflow Response:");
        Console.WriteLine(workflowResponse.Text);
    }

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

        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow,
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

    public static async Task WorkflowHumanCheckpointAsync()
    {
        //https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/GettingStarted/Workflows/Checkpoint/CheckpointWithHumanInTheLoop/Program.cs
        var workflow = WorkflowFactory.BuildWorkflow();

        var checkpointManager = CheckpointManager.Default;
        var checkpoints = new List<CheckpointInfo>();

        await using Checkpointed<StreamingRun> checkpointedRun = await InProcessExecution
            .StreamAsync(workflow, new SignalWithNumber(NumberSignal.Init), checkpointManager);

        await foreach (WorkflowEvent evt in checkpointedRun.Run.WatchStreamAsync())
        {
            switch (evt)
            {
                case RequestInfoEvent requestInputEvt:
                    ExternalResponse response = handleExternalRequest(requestInputEvt.Request);
                    await checkpointedRun.Run.SendResponseAsync(response);
                    break;
                case ExecutorCompletedEvent executorCompletedEvt:
                    Console.WriteLine($"* Executor {executorCompletedEvt.ExecutorId} completed.");
                    break;
                case SuperStepCompletedEvent superStepCompletedEvt:
                    // Checkpoints are automatically created at the end of each super step when a
                    // checkpoint manager is provided. You can store the checkpoint info for later use.
                    CheckpointInfo? checkpoint = superStepCompletedEvt.CompletionInfo!.Checkpoint;
                    if (checkpoint is not null)
                    {
                        checkpoints.Add(checkpoint);
                        Console.WriteLine($"** Checkpoint created at step {checkpoints.Count}.");
                    }
                    break;
                case WorkflowOutputEvent workflowOutputEvt:
                    Console.WriteLine($"Workflow completed with result: {workflowOutputEvt.Data}");
                    break;
            }
        }

        if (checkpoints.Count == 0)
            throw new InvalidOperationException("No checkpoints were created during the workflow execution.");
        Console.WriteLine($"Number of checkpoints created: {checkpoints.Count}");

        const int CheckpointIndex = 1;
        Console.WriteLine($"\n\nRestoring from the {CheckpointIndex + 1}th checkpoint.");
        CheckpointInfo savedCheckpoint = checkpoints[CheckpointIndex];
        
        await checkpointedRun.RestoreCheckpointAsync(savedCheckpoint, CancellationToken.None); // Note that we are restoring the state directly to the same run instance.
        await foreach (WorkflowEvent evt in checkpointedRun.Run.WatchStreamAsync())
        {
            switch (evt)
            {
                case RequestInfoEvent requestInputEvt:
                    // Handle `RequestInfoEvent` from the workflow
                    ExternalResponse response = handleExternalRequest(requestInputEvt.Request);
                    await checkpointedRun.Run.SendResponseAsync(response);
                    break;
                case ExecutorCompletedEvent executorCompletedEvt:
                    Console.WriteLine($"* Executor {executorCompletedEvt.ExecutorId} completed.");
                    break;
                case WorkflowOutputEvent workflowOutputEvt:
                    Console.WriteLine($"Workflow completed with result: {workflowOutputEvt.Data}");
                    break;
            }
        }
    }
}
