using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;

public sealed class HonchoHttpClient : IHonchoMemoryClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _http;
    private readonly HonchoMemoryOptions _options;
    private readonly ILogger<HonchoHttpClient> _logger;

    public HonchoHttpClient(
        HttpClient http,
        IOptions<HonchoMemoryOptions> options,
        ILogger<HonchoHttpClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsRemoteEnabled => _options.Enabled && _options.UseRemoteDialectic && _options.HasRemoteCredentials;

    public async Task EnsurePeerAsync(string peerId, CancellationToken ct = default)
    {
        if (!IsRemoteEnabled)
            return;

        await SendAsync(
            HttpMethod.Post,
            $"v2/workspaces/{Uri.EscapeDataString(_options.WorkspaceId)}/peers",
            new { id = peerId },
            ct).ConfigureAwait(false);
    }

    public async Task EnsureSessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (!IsRemoteEnabled)
            return;

        await SendAsync(
            HttpMethod.Post,
            $"v2/workspaces/{Uri.EscapeDataString(_options.WorkspaceId)}/sessions",
            new { id = sessionId },
            ct).ConfigureAwait(false);
    }

    public async Task AppendMessagesAsync(
        string sessionId,
        string userPeerId,
        string agentPeerId,
        string userMessage,
        string assistantMessage,
        CancellationToken ct = default)
    {
        if (!IsRemoteEnabled)
            return;

        var path =
            $"v2/workspaces/{Uri.EscapeDataString(_options.WorkspaceId)}/sessions/{Uri.EscapeDataString(sessionId)}/messages/";
        await SendAsync(
            HttpMethod.Post,
            path,
            new
            {
                messages = new object[]
                {
                    new { peer_id = userPeerId, content = userMessage },
                    new { peer_id = agentPeerId, content = assistantMessage }
                }
            },
            ct).ConfigureAwait(false);
    }

    public async Task<HonchoChatResult> ChatAsync(HonchoChatRequest request, CancellationToken ct = default)
    {
        if (!IsRemoteEnabled)
            return new HonchoChatResult(string.Empty, false);

        var path =
            $"v2/workspaces/{Uri.EscapeDataString(_options.WorkspaceId)}/peers/{Uri.EscapeDataString(request.UserId)}/chat";
        using var response = await SendAsync(
            HttpMethod.Post,
            path,
            new
            {
                query = request.Query,
                session_id = request.SessionId,
                stream = false
            },
            ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
        var content = doc.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String
            ? contentEl.GetString() ?? string.Empty
            : string.Empty;

        _logger.LogDebug("Honcho chat answered for peer {PeerId} session {SessionId}", request.UserId, request.SessionId);
        return new HonchoChatResult(content, true);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativeUrl, object body, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, relativeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("Libr4-AutonomousAppGeneration");
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await _http.SendAsync(request, ct).ConfigureAwait(false);
    }
}

public sealed class NullHonchoMemoryClient : IHonchoMemoryClient
{
    public bool IsRemoteEnabled => false;

    public Task EnsurePeerAsync(string peerId, CancellationToken ct = default) => Task.CompletedTask;

    public Task EnsureSessionAsync(string sessionId, CancellationToken ct = default) => Task.CompletedTask;

    public Task AppendMessagesAsync(
        string sessionId,
        string userPeerId,
        string agentPeerId,
        string userMessage,
        string assistantMessage,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task<HonchoChatResult> ChatAsync(HonchoChatRequest request, CancellationToken ct = default) =>
        Task.FromResult(new HonchoChatResult(string.Empty, false));
}
