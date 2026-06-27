using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBots.OpenCode;

public readonly record struct ServerSentEvent(string Type, string? Delta, bool IsIdle, string? SessionId);

public class OpenCodeClient : IDisposable
{
    static ServerSentEvent parseEvent(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var type = doc.RootElement.GetProperty("type").GetString() ?? "";
        string? delta = null;
        string? sessionId = null;
        var isIdle = false;

        if (doc.RootElement.TryGetProperty("properties", out var props))
        {
            if (props.TryGetProperty("delta", out var deltaProp))
                delta = deltaProp.GetString();

            if (props.TryGetProperty("sessionID", out var sidProp))
                sessionId = sidProp.GetString();

            if (props.TryGetProperty("status", out var status) &&
                status.TryGetProperty("type", out var statusType))
                isIdle = statusType.GetString() == "idle";
        }

        return new ServerSentEvent(type, delta, isIdle, sessionId);
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    readonly HttpClient httpClient;
    readonly string directory;
    readonly bool ownsHttpClient;
    bool serverStarted;

    public string BaseUrl { get; }
    public string Directory => directory;

    public OpenCodeClient(string? baseUrl = null, string? directory = null)
    {
        BaseUrl = (baseUrl ?? "http://127.0.0.1:4096").TrimEnd('/');
        this.directory = directory ?? System.IO.Directory.GetCurrentDirectory();
        httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        ownsHttpClient = true;
    }

    string withDir(string endpoint) =>
        $"{endpoint}{(endpoint.Contains('?') ? "&" : "?")}directory={Uri.EscapeDataString(directory)}";

    async Task<bool> checkHealthAsync(CancellationToken ct)
    {
        try
        {
            using var resp = await httpClient.GetAsync("/", ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    void launchProcess()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd",
            Arguments = "/c start opencode serve",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = directory
        });
    }

    async Task<bool> waitForHealthAsync(CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        var linked = linkedCts.Token;

        while (!linked.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(500, linked);
                using var resp = await httpClient.GetAsync("/", linked);
                if (resp.IsSuccessStatusCode) return true;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested) { return false; }
            catch { }
        }
        return false;
    }

    public async Task EnsureRunningAsync(CancellationToken ct = default)
    {
        if (serverStarted) return;

        var running = await checkHealthAsync(ct);
        if (!running)
        {
            launchProcess();
            running = await waitForHealthAsync(ct);
        }

        if (!running)
            throw new TimeoutException("OpenCode server did not start within 30 seconds.");

        serverStarted = true;
    }

    public async Task<SessionRecord> CreateSessionAsync(CancellationToken ct = default)
    {
        await EnsureRunningAsync(ct);
        var response = await httpClient.PostAsJsonAsync(withDir("/session"),
            new CreateSessionRequest { Title = "ChatBots" }, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionRecord>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize session");
    }

    public async Task<MessageWithParts> SendMessageAsync(
        string sessionId, SendMessageRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            withDir($"/session/{sessionId}/message"), request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageWithParts>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize message response");
    }

    public async Task SendMessageNonBlockingAsync(
        string sessionId, SendMessageRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            withDir($"/session/{sessionId}/prompt_async"), request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
    }

    public async IAsyncEnumerable<ServerSentEvent> SubscribeToEventsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get, withDir("/event"));
        request.Headers.Accept.ParseAdd("text/event-stream");

        using var response = await httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var dataBuilder = new StringBuilder();

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (dataBuilder.Length > 0)
                {
                    var json = dataBuilder.ToString();
                    dataBuilder.Clear();
                    yield return parseEvent(json);
                }
                continue;
            }

            if (line.StartsWith("data: "))
                dataBuilder.Append(line.AsSpan(6));
        }
    }

    public void Dispose()
    {
        if (ownsHttpClient) httpClient.Dispose();
    }
}
