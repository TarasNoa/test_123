using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Api;

/// <summary>
/// WebSocket channel <c>/ws/run-sync/{runId}</c> for live workspace sync while local + cloud runs are active.
/// </summary>
public sealed class RunSyncWebSocketHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RunSyncHub _hub;
    private readonly IRunSyncCoordinator _coordinator;
    private readonly RunSyncBridgeHostedService _bridge;
    private readonly ILogger<RunSyncWebSocketHandler> _logger;

    public RunSyncWebSocketHandler(
        RunSyncHub hub,
        IRunSyncCoordinator coordinator,
        RunSyncBridgeHostedService bridge,
        ILogger<RunSyncWebSocketHandler> logger)
    {
        _hub = hub;
        _coordinator = coordinator;
        _bridge = bridge;
        _logger = logger;
    }

    public async Task HandleWebSocketAsync(HttpContext context, Guid runId, string role, string? workspaceRoot)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("workspaceRoot query parameter required");
            return;
        }

        _coordinator.RegisterSession(runId, workspaceRoot, role);
        _bridge.StartWatching(runId);
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString("N");

        async Task SendAsync(WorkspaceSyncDelta delta)
        {
            if (socket.State != WebSocketState.Open)
                return;

            var json = JsonSerializer.Serialize(new
            {
                type = "delta",
                delta
            }, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        _hub.RegisterConnection(runId, connectionId, SendAsync);
        _logger.LogInformation("Run sync WebSocket connected run={RunId} role={Role}", runId, role);

        try
        {
            var buffer = new byte[1024 * 64];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await HandleMessageAsync(runId, connectionId, json).ConfigureAwait(false);
            }
        }
        finally
        {
            _hub.UnregisterConnection(runId, connectionId);
            _bridge.StopWatching(runId);
            _coordinator.UnregisterSession(runId);
            _logger.LogInformation("Run sync WebSocket disconnected run={RunId}", runId);
        }
    }

    private async Task HandleMessageAsync(Guid runId, string connectionId, string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeEl))
                return;

            if (!string.Equals(typeEl.GetString(), "delta", StringComparison.OrdinalIgnoreCase))
                return;

            if (!root.TryGetProperty("delta", out var deltaEl))
                return;

            var delta = deltaEl.Deserialize<WorkspaceSyncDelta>(JsonOptions);
            if (delta is null || delta.RunId != runId)
                return;

            var apply = await _hub.IngestDeltaAsync(delta, connectionId).ConfigureAwait(false);
            _logger.LogDebug(
                "Run sync delta ingested run={RunId} path={Path} status={Status}",
                runId,
                delta.RelativePath,
                apply.Status);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Invalid run sync websocket payload run={RunId}", runId);
        }
    }
}
