using System.Net.WebSockets;
using Libr4.IDE.Application.Terminal;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

using Microsoft.Extensions.Logging;

/// <summary>
/// WebSocket handler for real-time terminal output
/// </summary>
public class TerminalWebSocketHandler
{
    private readonly Dictionary<string, List<WebSocket>> _connections = new();
    private readonly Dictionary<string, System.Threading.CancellationTokenSource> _outputTasks = new();
    private readonly object _lock = new();
    private readonly ITerminalService _terminalService;
    private readonly IDockerService _dockerService;
    private readonly ILogger<TerminalWebSocketHandler> _logger;

    public TerminalWebSocketHandler(ITerminalService terminalService, IDockerService dockerService, ILogger<TerminalWebSocketHandler> logger)
    {
        _terminalService = terminalService;
        _dockerService = dockerService;
        _logger = logger;
    }

    public async Task HandleWebSocketAsync(HttpContext context, string sessionId)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        lock (_lock)
        {
            if (!_connections.ContainsKey(sessionId))
            {
                _connections[sessionId] = new List<WebSocket>();
            }
            _connections[sessionId].Add(webSocket);
        }

        // Start output streaming task
        var cts = new System.Threading.CancellationTokenSource();
        lock (_lock)
        {
            _outputTasks[sessionId] = cts;
        }

        // Ensure session exists (create on-the-fly if client connects before explicit creation)
        try
        {
            var session = await _terminalService.GetSessionAsync(sessionId, cts.Token);
            if (session is null)
            {
                await _terminalService.CreateSessionAsync("default", ct: cts.Token, sessionId: sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-create terminal session {SessionId}", sessionId);
        }

        var outputTask = StreamOutputAsync(sessionId, cts.Token);

        var buffer = new byte[1024 * 4];
        
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }
                
                // Handle input from client (e.g., user typing in terminal)
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // Write keystrokes to shell stdin for interactive terminal
                    try
                    {
                        await _dockerService.WriteToShellAsync(sessionId, message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to write to shell {SessionId}", sessionId);
                    }
                }
            }
        }
        finally
        {
            cts.Cancel();
            await outputTask;
            
            lock (_lock)
            {
                _connections[sessionId].Remove(webSocket);
                if (_connections[sessionId].Count == 0)
                {
                    _connections.Remove(sessionId);
                    _outputTasks.Remove(sessionId);
                }
            }
        }
    }

    private async Task StreamOutputAsync(string sessionId, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Poll for new output from terminal
                var output = await _terminalService.GetOutputAsync(sessionId, ct);
                
                if (!string.IsNullOrEmpty(output))
                {
                    await BroadcastToSession(sessionId, output);
                }

                await Task.Delay(100, ct); // Poll every 100ms
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    public async Task BroadcastToSession(string sessionId, string message)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
        List<WebSocket> connectionsToSend;

        lock (_lock)
        {
            if (!_connections.ContainsKey(sessionId)) return;
            connectionsToSend = new List<WebSocket>(_connections[sessionId]);
        }

        var tasks = connectionsToSend.Where(ws => ws.State == WebSocketState.Open)
            .Select(ws => ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None));

        await Task.WhenAll(tasks);
    }

    public int GetConnectionCount(string sessionId)
    {
        lock (_lock)
        {
            return _connections.ContainsKey(sessionId) ? _connections[sessionId].Count : 0;
        }
    }

    private static string FormatOutput(Libr4.AI.Domain.Terminal.CommandEntry entry)
    {
        return $"$ {entry.Command}\n{entry.Output}\n[Exit: {entry.ExitCode}] [{entry.DurationMs}ms]";
    }
}
