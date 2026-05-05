/*
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libr4.Shared.Infrastructure.WebSocket;

/// <summary>
/// C# Bridge for Rust WebSocket Server (gRPC/HTTP fallback)
/// Golden Stack: C# calls Rust for high-performance WebSocket handling
/// </summary>
public interface IRustWebSocketBridge
{
    Task ConnectAsync(string url, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task SendAsync(string channel, object payload, CancellationToken ct = default);
    Task SubscribeAsync(string channel, CancellationToken ct = default);
    Task UnsubscribeAsync(string channel, CancellationToken ct = default);
    event EventHandler<WebSocketMessage>? OnMessage;
    event EventHandler? OnConnected;
    event EventHandler? OnDisconnected;
    bool IsConnected { get; }
}

/// <summary>
/// WebSocket message from Rust server
/// </summary>
public class WebSocketMessage
{
    public string Channel { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Bridge implementation connecting to Rust WebSocket server
/// </summary>
public class RustWebSocketBridge : IRustWebSocketBridge, IDisposable
{
    private ClientWebSocket? _webSocket;
    private readonly ILogger<RustWebSocketBridge> _logger;
    private readonly string _rustWsUrl;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public event EventHandler<WebSocketMessage>? OnMessage;
    public event EventHandler? OnConnected;
    public event EventHandler? OnDisconnected;

    public RustWebSocketBridge(
        string rustWebSocketUrl,
        ILogger<RustWebSocketBridge> logger)
    {
        _rustWsUrl = rustWebSocketUrl;
        _logger = logger;
    }

    public async Task ConnectAsync(string url, CancellationToken ct = default)
    {
        try
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            
            _logger.LogInformation("Connecting to Rust WebSocket server at {Url}", _rustWsUrl);
            
            await _webSocket.ConnectAsync(new Uri(_rustWsUrl), ct);
            
            _receiveCts = new CancellationTokenSource();
            _receiveTask = ReceiveLoopAsync(_receiveCts.Token);
            
            _logger.LogInformation("Connected to Rust WebSocket server");
            OnConnected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Rust WebSocket server");
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            _receiveCts?.Cancel();
            
            if (_receiveTask != null)
            {
                try
                {
                    await _receiveTask.WaitAsync(TimeSpan.FromSeconds(5), ct);
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Receive task did not complete in time");
                }
            }

            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Disconnecting",
                    ct);
            }

            _webSocket?.Dispose();
            _webSocket = null;
            
            _logger.LogInformation("Disconnected from Rust WebSocket server");
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during WebSocket disconnect");
        }
    }

    public async Task SendAsync(string channel, object payload, CancellationToken ct = default)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected");
        }

        var message = new
        {
            channel,
            payload,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            ct);

        _logger.LogDebug("Sent message to channel {Channel}", channel);
    }

    public Task SubscribeAsync(string channel, CancellationToken ct = default)
    {
        return SendAsync("system:subscribe", new { channel }, ct);
    }

    public Task UnsubscribeAsync(string channel, CancellationToken ct = default)
    {
        return SendAsync("system:unsubscribe", new { channel }, ct);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[4096];

        try
        {
            while (!ct.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                var result = await _webSocket!.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket closed by server");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    
                    try
                    {
                        var message = JsonSerializer.Deserialize<WebSocketMessage>(json);
                        if (message != null)
                        {
                            message.Timestamp = DateTime.UtcNow;
                            OnMessage?.Invoke(this, message);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize WebSocket message: {Json}", json);
                    }
                }

                if (result.MessageType == WebSocketMessageType.Ping)
                {
                    await _webSocket.SendAsync(
                        new ArraySegment<byte>([]),
                        WebSocketMessageType.Pong,
                        true,
                        ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("WebSocket receive loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket receive loop");
        }
        finally
        {
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
        _receiveCts?.Dispose();
        _webSocket?.Dispose();
    }
}

/// <summary>
/// DI Extensions
/// </summary>
public static class RustWebSocketExtensions
{
    public static IServiceCollection AddRustWebSocket(
        this IServiceCollection services,
        string rustWebSocketUrl)
    {
        services.AddSingleton<IRustWebSocketBridge>(sp =>
            new RustWebSocketBridge(
                rustWebSocketUrl,
                sp.GetRequiredService<ILogger<RustWebSocketBridge>>()));

        return services;
    }
}
*/
