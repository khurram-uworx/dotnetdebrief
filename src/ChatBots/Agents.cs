using AgentFramework;
using GitHub.Copilot.SDK;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        var signal = request.Data.As<SignalWithNumber>();

        if (signal is not null)
        {
            switch (signal.Signal)
            {
                case NumberSignal.Init:
                    int initialGuess = getIntegerFromHuman("Please provide your initial guess: ");
                    return new ExternalResponse(request.PortInfo, request.RequestId, new PortableValue(initialGuess));
                case NumberSignal.Above:
                    int lowerGuess = getIntegerFromHuman($"You previously guessed {signal.Number} too large. Please provide a new guess: ");
                    return new ExternalResponse(request.PortInfo, request.RequestId, new PortableValue(lowerGuess));
                case NumberSignal.Below:
                    int higherGuess = getIntegerFromHuman($"You previously guessed {signal.Number} too small. Please provide a new guess: ");
                    return new ExternalResponse(request.PortInfo, request.RequestId, new PortableValue(higherGuess));
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

        var response = await writer.RunAsync(prompt);
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
        AIAgent workflowAgent = workflow.AsAIAgent();
        var workflowResponse = await workflowAgent.RunAsync(prompt);

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

    public static async Task WorkflowHumanCheckpointAsync()
    {
        //https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/GettingStarted/Workflows/Checkpoint/CheckpointWithHumanInTheLoop/Program.cs
        var workflow = WorkflowFactory.BuildWorkflow();

        var checkpointManager = CheckpointManager.Default;
        var checkpoints = new List<CheckpointInfo>();

        StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow,
            new SignalWithNumber(NumberSignal.Init),
            checkpointManager);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case RequestInfoEvent requestInputEvt:
                    ExternalResponse response = handleExternalRequest(requestInputEvt.Request);
                    await run.SendResponseAsync(response);
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

        StreamingRun restoredRun = await InProcessExecution.ResumeStreamingAsync(workflow, savedCheckpoint, checkpointManager);
        await foreach (WorkflowEvent evt in restoredRun.WatchStreamAsync())
        {
            switch (evt)
            {
                case RequestInfoEvent requestInputEvt:
                    // Handle `RequestInfoEvent` from the workflow
                    ExternalResponse response = handleExternalRequest(requestInputEvt.Request);
                    await restoredRun.SendResponseAsync(response);
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

    public static async Task CopilotAgentWithToolsAsync()
    {
        //https://github.com/github/copilot-sdk/blob/main/docs/integrations/microsoft-agent-framework.md
        // Define a custom tool
        AIFunction weatherTool = AIFunctionFactory.Create(
            (string location) => $"The weather in {location} is sunny with a high of 25°C.",
            "GetWeather",
            "Get the current weather for a given location."
        );

        await using var copilotClient = new CopilotClient();
        await copilotClient.StartAsync();

        //var done = new TaskCompletionSource(); // we can await done.Task;
        //copilotClient.On(evt =>
        //{
        //    //if (evt is AssistantMessageEvent msg)
        //    //    Console.WriteLine(msg.Data.Content);
        //    if (evt is SessionIdleEvent)
        //        done.SetResult();
        //});

        var sessionConfig = new SessionConfig
        {
            OnPermissionRequest = (req, inv) =>
            {
                Console.WriteLine($"\n[Permission Request: {req.Kind}]");

                //Console.Write("Approve? (y/n): ");
                //string? input = Console.ReadLine()?.Trim().ToUpperInvariant();
                //string kind = input is "Y" or "YES" ? "approved" : "denied-interactively-by-user";

                return Task.FromResult(new PermissionRequestResult()
                {
                    Kind = PermissionRequestResultKind.Approved
                });
            },
            Tools = [weatherTool]
        };

        AIAgent agent = copilotClient.AsAIAgent(sessionConfig);
        var response = await agent.RunAsync("What's the weather like in Faisalabad?");
        
        Console.WriteLine(response);
    }
}
