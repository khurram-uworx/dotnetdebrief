using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChatBots.OpenCode;

record PartInput
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    public static PartInput TextInput(string text) => new() { Type = "text", Text = text };
}

record ModelReference
{
    [JsonPropertyName("providerID")]
    public string ProviderId { get; init; } = "";

    [JsonPropertyName("modelID")]
    public string ModelId { get; init; } = "";
}

record SendMessageRequest
{
    [JsonPropertyName("parts")]
    public List<PartInput> Parts { get; init; } = [];

    [JsonPropertyName("model")]
    public ModelReference? Model { get; init; }
}

record Part
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

record MessageTime
{
    [JsonPropertyName("created")]
    public long Created { get; init; }
}

record MessageInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("role")]
    public string? Role { get; init; }

    [JsonPropertyName("time")]
    public MessageTime? Time { get; init; }
}

record MessageWithParts
{
    [JsonPropertyName("info")]
    public MessageInfo? Message { get; init; }

    [JsonPropertyName("parts")]
    public List<Part>? Parts { get; init; }
}

record SessionRecord
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("directory")]
    public string? Directory { get; init; }
}

class OpenCodeChatClient : IChatClient
{
    static SendMessageRequest buildRequest(IEnumerable<ChatMessage> chatMessages)
    {
        var messages = chatMessages.ToList();
        var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User)
            ?? throw new ArgumentException("No user message found", nameof(chatMessages));

        var parts = new List<PartInput>();
        foreach (var content in lastUserMessage.Contents)
        {
            if (content is TextContent text)
                parts.Add(PartInput.TextInput(text.Text ?? ""));
        }
        if (parts.Count == 0)
            parts.Add(PartInput.TextInput(lastUserMessage.Text ?? ""));

        return new SendMessageRequest { Parts = parts };
    }

    static ChatResponse toChatResponse(MessageWithParts response)
    {
        var text = response.Parts?.FirstOrDefault(p => p.Type == "text")?.Text ?? "";
        var chatMessage = new ChatMessage(ChatRole.Assistant, text);

        return new ChatResponse(chatMessage)
        {
            ResponseId = response.Message?.Id ?? "",
            CreatedAt = response.Message?.Time is not null
                ? DateTimeOffset.FromUnixTimeMilliseconds(response.Message.Time.Created)
                : DateTimeOffset.UtcNow
        };
    }

    readonly OpenCodeClient client;
    string? sessionId;

    public ChatClientMetadata Metadata { get; }

    public string? SessionId
    {
        get => sessionId;
        set => sessionId = value;
    }

    public OpenCodeChatClient(OpenCodeClient client)
    {
        this.client = client;
        Metadata = new ChatClientMetadata("OpenCode", new Uri(client.BaseUrl), "opencode");
    }

    async Task<SessionRecord> ensureSessionAsync(CancellationToken ct)
    {
        if (sessionId is null)
        {
            var session = await client.CreateSessionAsync(ct);
            sessionId = session.Id;
            return session;
        }
        return new SessionRecord { Id = sessionId };
    }

    async Task consumeEventsAsync(ChannelWriter<ServerSentEvent> writer, CancellationToken ct)
    {
        try
        {
            await foreach (var evt in client.SubscribeToEventsAsync(ct))
            {
                if (!writer.TryWrite(evt))
                    break;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SSE error: {ex.Message}");
        }
        finally { writer.TryComplete(); }
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var session = await ensureSessionAsync(cancellationToken);
        var request = buildRequest(chatMessages);
        var response = await client.SendMessageAsync(session.Id, request, cancellationToken);
        return toChatResponse(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var session = await ensureSessionAsync(cancellationToken);
        var request = buildRequest(chatMessages);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var channel = Channel.CreateUnbounded<ServerSentEvent>();

        // Start SSE subscription in background before sending prompt
        var consumeTask = Task.Run(() => consumeEventsAsync(channel.Writer, cts.Token), cts.Token);

        // Give the SSE connection a moment to establish
        await Task.Delay(100, cts.Token);

        // Send the prompt (non-blocking)
        await client.SendMessageNonBlockingAsync(session.Id, request, cts.Token);

        // Read events from channel and yield text deltas
        await foreach (var evt in channel.Reader.ReadAllAsync(cts.Token))
        {
            // Only process events for our session
            if (evt.SessionId is not null && evt.SessionId != session.Id)
                continue;

            if (evt.Delta is not null)
            {
                yield return new ChatResponseUpdate
                {
                    Contents = [new TextContent(evt.Delta)],
                    Role = ChatRole.Assistant
                };
            }

            if (evt.IsIdle)
                break;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceKey is null && serviceType == typeof(OpenCodeClient) ? client : null;
    }

    public void Dispose() { }
}
