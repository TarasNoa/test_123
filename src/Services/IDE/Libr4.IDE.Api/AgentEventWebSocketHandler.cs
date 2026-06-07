using System.Net.WebSockets;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Api;

/// <summary>
/// WebSocket handler for real-time agent event delivery to frontend
/// </summary>
public class AgentEventWebSocketHandler
{
    private readonly Dictionary<string, List<WebSocket>> _connections = new();
    private readonly IAgentEventEmitter _eventEmitter;
    private readonly IAgentOrchestrationTracker _orchestrationTracker;
    private readonly ILogger<AgentEventWebSocketHandler> _logger;
    private readonly object _lock = new();

    public AgentEventWebSocketHandler(
        IAgentEventEmitter eventEmitter,
        IAgentOrchestrationTracker orchestrationTracker,
        ILogger<AgentEventWebSocketHandler> logger)
    {
        _eventEmitter = eventEmitter;
        _orchestrationTracker = orchestrationTracker;
        _logger = logger;
    }

    public async Task HandleWebSocketAsync(HttpContext context, string runId)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        lock (_lock)
        {
            if (!_connections.ContainsKey(runId))
            {
                _connections[runId] = new List<WebSocket>();
            }
            _connections[runId].Add(webSocket);
        }

        _logger.LogInformation("WebSocket connected for run {RunId}", runId);

        try
        {
            var buffer = new byte[1024 * 4];
            
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }
            }
        }
        finally
        {
            lock (_lock)
            {
                _connections[runId].Remove(webSocket);
                if (_connections[runId].Count == 0)
                {
                    _connections.Remove(runId);
                }
            }
        }
    }

    public async Task BroadcastEventAsync(AgentEvent evt)
    {
        var message = new
        {
            type = GetEventTypeString(evt.Type),
            runId = evt.RunId,
            command = evt.Command,
            output = evt.Output,
            exitCode = evt.ExitCode,
            durationMs = evt.DurationMs,
            timestamp = evt.Timestamp
        };

        await BroadcastToRun(evt.RunId, message);
    }

    public async Task BroadcastRuntimeNdjsonAsync(AgentRuntimePublishedEvent evt)
    {
        object message;
        try
        {
            using var doc = JsonDocument.Parse(evt.PayloadJson);
            message = new Dictionary<string, object?>
            {
                ["type"] = evt.EventType,
                ["runId"] = evt.RunId,
                ["timestamp"] = evt.TimestampUtc.ToUnixTimeMilliseconds(),
                ["payload"] = doc.RootElement.Clone()
            };
        }
        catch
        {
            message = new
            {
                type = evt.EventType,
                runId = evt.RunId,
                timestamp = evt.TimestampUtc.ToUnixTimeMilliseconds(),
                raw = evt.PayloadJson
            };
        }

        await BroadcastToRun(evt.RunId, message);
    }

    public async Task BroadcastOrchestrationAsync(AgentOrchestrationEvent evt)
    {
        var message = new
        {
            type = "agent-call",
            runId = evt.RunId,
            orchestration = evt,
            timestamp = evt.Timestamp
        };

        await BroadcastToRun(evt.RunId, message);
    }

    private async Task BroadcastToRun(Guid runId, object message)
    {
        List<WebSocket> connectionsToSend;
        
        lock (_lock)
        {
            if (!_connections.ContainsKey(runId.ToString())) return;
            connectionsToSend = new List<WebSocket>(_connections[runId.ToString()]);
        }

        var json = JsonSerializer.Serialize(message);
        var buffer = System.Text.Encoding.UTF8.GetBytes(json);

        var tasks = connectionsToSend.Where(ws => ws.State == WebSocketState.Open)
            .Select(ws => ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None));

        await Task.WhenAll(tasks);

        _logger.LogDebug("Broadcasted event to {Count} clients for run {RunId}", connectionsToSend.Count, runId);
    }

    private static string GetEventTypeString(AgentEventType type)
    {
        return type switch
        {
            AgentEventType.BuildStart => "build-start",
            AgentEventType.BuildComplete => "build-complete",
            AgentEventType.TestStart => "test-start",
            AgentEventType.TestComplete => "test-complete",
            AgentEventType.SecurityScanStart => "security-scan",
            AgentEventType.SecurityScanComplete => "security-complete",
            AgentEventType.TerminalOutput => "terminal-output",
            AgentEventType.BrowserLaunch => "browser-launch",
            AgentEventType.BrowserNavigate => "browser-navigate",
            AgentEventType.BrowserScreenshot => "browser-screenshot",
            AgentEventType.BrowserExecuteJavaScript => "browser-execute-js",
            AgentEventType.BrowserClose => "browser-close",
            AgentEventType.BrowserSnapshot => "browser-snapshot",
            AgentEventType.BrowserClick => "browser-click",
            AgentEventType.BrowserType => "browser-type",
            AgentEventType.BrowserScroll => "browser-scroll",
            AgentEventType.BrowserWait => "browser-wait",
            AgentEventType.BrowserConsole => "browser-console",
            AgentEventType.BrowserGetContent => "browser-get-content",
            AgentEventType.BrowserExtract => "browser-extract",
            AgentEventType.BrowserRecordStart => "browser-record-start",
            AgentEventType.BrowserRecordStop => "browser-record-stop",
            AgentEventType.RuntimeNdjson => "runtime-ndjson",
            _ => "unknown"
        };
    }

    public int GetConnectionCount(Guid runId)
    {
        lock (_lock)
        {
            return _connections.TryGetValue(runId.ToString(), out var connections) ? connections.Count : 0;
        }
    }
}
