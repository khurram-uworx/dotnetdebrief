using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AgentFramework;

enum NumberSignal { Init, Above, Below }

static class WorkflowFactory
{
    public static Workflow BuildWorkflow()
    {
        RequestPort numberRequest = RequestPort.Create<SignalWithNumber, int>("GuessNumber");
        JudgeExecutor judgeExecutor = new(42);

        return new WorkflowBuilder(numberRequest)

            .AddEdge(numberRequest, judgeExecutor)
            .AddEdge(judgeExecutor, numberRequest)
            
            .WithOutputFrom(judgeExecutor)
            .Build();
    }
}

class SignalWithNumber
{
    public NumberSignal Signal { get; }
    public int? Number { get; }

    public SignalWithNumber(NumberSignal signal, int? number = null)
    {
        this.Signal = signal;
        this.Number = number;
    }
}

class SloganResult
{
    [JsonPropertyName("task")]
    public required string Task { get; set; }

    [JsonPropertyName("slogan")]
    public required string Slogan { get; set; }
}

class FeedbackResult
{
    [JsonPropertyName("comments")]
    public string Comments { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    [JsonPropertyName("actions")]
    public string Actions { get; set; } = string.Empty;
}

class SloganGeneratedEvent(SloganResult sloganResult) : WorkflowEvent(sloganResult)
{
    public override string ToString() => $"Slogan: {sloganResult.Slogan}";
}

class FeedbackEvent(FeedbackResult feedbackResult) : WorkflowEvent(feedbackResult)
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    public override string ToString() => $"Feedback:\n{JsonSerializer.Serialize(feedbackResult, this._options)}";
}

class SloganWriterExecutor : Executor
{
    readonly AIAgent agent;
    readonly AgentThread thread;

    public SloganWriterExecutor(string id, IChatClient chatClient) : base(id)
    {
        ChatClientAgentOptions agentOptions = new(instructions: "You are a professional slogan writer. You will be given a task to create a slogan.")
        {
            ChatOptions = new()
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<SloganResult>()
            }
        };

        this.agent = new ChatClientAgent(chatClient, agentOptions);
        this.thread = this.agent.GetNewThread();
    }

    protected override RouteBuilder ConfigureRoutes(RouteBuilder routeBuilder) =>
        routeBuilder.AddHandler<string, SloganResult>(this.HandleAsync)
                    .AddHandler<FeedbackResult, SloganResult>(this.HandleAsync);

    public async ValueTask<SloganResult> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var result = await this.agent.RunAsync(message, this.thread, cancellationToken: cancellationToken);

        var sloganResult = JsonSerializer.Deserialize<SloganResult>(result.Text) ?? throw new InvalidOperationException("Failed to deserialize slogan result.");

        await context.AddEventAsync(new SloganGeneratedEvent(sloganResult), cancellationToken);
        return sloganResult;
    }

    public async ValueTask<SloganResult> HandleAsync(FeedbackResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var feedbackMessage = $"""
            Here is the feedback on your previous slogan:
            Comments: {message.Comments}
            Rating: {message.Rating}
            Suggested Actions: {message.Actions}

            Please use this feedback to improve your slogan.
            """;

        var result = await this.agent.RunAsync(feedbackMessage, this.thread, cancellationToken: cancellationToken);
        var sloganResult = JsonSerializer.Deserialize<SloganResult>(result.Text) ?? throw new InvalidOperationException("Failed to deserialize slogan result.");

        await context.AddEventAsync(new SloganGeneratedEvent(sloganResult), cancellationToken);
        return sloganResult;
    }
}

class FeedbackExecutor : Executor<SloganResult>
{
    readonly AIAgent agent;
    readonly AgentThread thread;
    int attempts;

    public int MinimumRating { get; init; } = 7;

    public int MaxAttempts { get; init; } = 30;

    public FeedbackExecutor(string id, IChatClient chatClient) : base(id)
    {
        ChatClientAgentOptions agentOptions = new(instructions: "You are a professional editor. You will be given a slogan and the task it is meant to accomplish.")
        {
            ChatOptions = new()
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<FeedbackResult>()
            }
        };

        this.agent = new ChatClientAgent(chatClient, agentOptions);
        this.thread = this.agent.GetNewThread();
    }

    public override async ValueTask HandleAsync(SloganResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var sloganMessage = $"""
            Here is a slogan for the task '{message.Task}':
            Slogan: {message.Slogan}
            Please provide feedback on this slogan, including comments, a rating from 1 to 10, and suggested actions for improvement.
            """;

        var response = await this.agent.RunAsync(sloganMessage, this.thread, cancellationToken: cancellationToken);
        var feedback = JsonSerializer.Deserialize<FeedbackResult>(response.Text) ?? throw new InvalidOperationException("Failed to deserialize feedback.");

        await context.AddEventAsync(new FeedbackEvent(feedback), cancellationToken);

        if (feedback.Rating >= this.MinimumRating)
        {
            await context.YieldOutputAsync($"The following slogan was accepted:\n\n{message.Slogan}", cancellationToken);
            return;
        }

        if (this.attempts >= this.MaxAttempts)
        {
            await context.YieldOutputAsync($"The slogan was rejected after {this.MaxAttempts} attempts. Final slogan:\n\n{message.Slogan}", cancellationToken);
            return;
        }

        await context.SendMessageAsync(feedback, cancellationToken: cancellationToken);
        this.attempts++;
    }
}

class JudgeExecutor() : Executor<int>("Judge")
{
    const string StateKey = "JudgeExecutorState";

    readonly int targetNumber;
    int tries;

    public JudgeExecutor(int targetNumber) : this() =>
        (this.targetNumber) = targetNumber;

    public override async ValueTask HandleAsync(int message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        this.tries++;
        if (message == this.targetNumber)
            await context.YieldOutputAsync($"{this.targetNumber} found in {this.tries} tries!", cancellationToken);
        else if (message < this.targetNumber)
            await context.SendMessageAsync(new SignalWithNumber(NumberSignal.Below, message), cancellationToken: cancellationToken);
        else
            await context.SendMessageAsync(new SignalWithNumber(NumberSignal.Above, message), cancellationToken: cancellationToken);
    }

    protected override ValueTask OnCheckpointingAsync(IWorkflowContext context, CancellationToken cancellationToken = default) =>
        context.QueueStateUpdateAsync(StateKey, this.tries, cancellationToken: cancellationToken);

    protected override async ValueTask OnCheckpointRestoredAsync(IWorkflowContext context, CancellationToken cancellationToken = default) =>
        this.tries = await context.ReadStateAsync<int>(StateKey, cancellationToken: cancellationToken);
}
