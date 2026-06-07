using System.Net.Http.Json;
using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;

/// <summary>
/// HTTP JSON-RPC transport for remote MCP servers (SSE profile posts to /message).
/// </summary>
public sealed class McpSseTransport
{
    private readonly HttpClient _http;
    private readonly McpSseServerProfile _profile;
    private int _nextId = 1;

    public McpSseTransport(HttpClient http, McpSseServerProfile profile)
    {
        _http = http;
        _profile = profile;
    }

    public async Task<JsonElement> CallToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        var id = Interlocked.Increment(ref _nextId);
        var payload = new
        {
            jsonrpc = "2.0",
            id,
            method = "tools/call",
            @params = new { name = toolName, arguments },
        };

        using var response = await _http.PostAsJsonAsync(_profile.MessagePath.TrimStart('/'), payload, cts.Token)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token)
            .ConfigureAwait(false);
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out var err))
        {
            var msg = err.TryGetProperty("message", out var m) ? m.GetString() : err.ToString();
            throw new InvalidOperationException($"MCP SSE error: {msg}");
        }

        return root.GetProperty("result");
    }

    public async Task<bool> ProbeAsync(TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            var id = Interlocked.Increment(ref _nextId);
            var payload = new
            {
                jsonrpc = "2.0",
                id,
                method = "tools/list",
                @params = new { },
            };
            using var response = await _http.PostAsJsonAsync(_profile.MessagePath.TrimStart('/'), payload, cts.Token)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
