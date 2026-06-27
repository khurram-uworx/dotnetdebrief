using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatBots.OpenCode;

public record SessionRecord
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("directory")]
    public string? Directory { get; init; }
}

public record CreateSessionRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

public record SendMessageRequest
{
    [JsonPropertyName("parts")]
    public List<PartInput> Parts { get; init; } = [];

    [JsonPropertyName("model")]
    public ModelReference? Model { get; init; }
}

public record MessageWithParts
{
    [JsonPropertyName("info")]
    public MessageInfo? Message { get; init; }

    [JsonPropertyName("parts")]
    public List<Part>? Parts { get; init; }
}

public record MessageInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("role")]
    public string? Role { get; init; }

    [JsonPropertyName("time")]
    public MessageTime? Time { get; init; }
}

public record MessageTime
{
    [JsonPropertyName("created")]
    public long Created { get; init; }
}

public record Part
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

public record PartInput
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    public static PartInput TextInput(string text) => new() { Type = "text", Text = text };
}

public record ModelReference
{
    [JsonPropertyName("providerID")]
    public string ProviderId { get; init; } = "";

    [JsonPropertyName("modelID")]
    public string ModelId { get; init; } = "";
}
