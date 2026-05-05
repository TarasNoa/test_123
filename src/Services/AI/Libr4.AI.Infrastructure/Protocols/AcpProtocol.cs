using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;
using System.Text.Json;

namespace Libr4.AI.Infrastructure.Protocols;

/// <summary>
/// Agent Communication Protocol (ACP) for inter-agent messaging.
/// Lightweight protocol for agent-to-agent communication.
/// </summary>
public interface IAcpProtocol
{
    Task SendMessageAsync(AcpMessage message, CancellationToken ct = default);
    IAsyncEnumerable<AcpMessage> ReceiveMessagesAsync(string agentId, CancellationToken ct = default);
    Task<AcpResponse> RequestAsync(AcpRequest request, TimeSpan? timeout = null, CancellationToken ct = default);
}

/// <summary>
/// ACP implementation over message bus (RabbitMQ/MassTransit).
/// </summary>
public sealed class AcpProtocol : IAcpProtocol
{
    private readonly IAcpTransport _transport;
    private readonly IAcpMessageStore _store;
    private readonly ILogger<AcpProtocol> _logger;

    public AcpProtocol(
        IAcpTransport transport,
        IAcpMessageStore store,
        ILogger<AcpProtocol> logger)
    {
        _transport = transport;
        _store = store;
        _logger = logger;
    }

    public async Task SendMessageAsync(AcpMessage message, CancellationToken ct = default)
    {
        message.Timestamp = DateTime.UtcNow;
        message.MessageId ??= Guid.NewGuid().ToString();

        await _store.StoreAsync(message, ct);
        await _transport.PublishAsync(message, ct);

        _logger.LogDebug("ACP message sent: {MessageId} from {From} to {To}",
            message.MessageId, message.FromAgentId, message.ToAgentId);
    }

    public async IAsyncEnumerable<AcpMessage> ReceiveMessagesAsync(
        string agentId, 
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var message in _transport.SubscribeAsync(agentId, ct))
        {
            if (message.ToAgentId == agentId || message.ToAgentId == "*")
            {
                yield return message;
            }
        }
    }

    public async Task<AcpResponse> RequestAsync(
        AcpRequest request, 
        TimeSpan? timeout = null, 
        CancellationToken ct = default)
    {
        var message = new AcpMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            FromAgentId = request.FromAgentId,
            ToAgentId = request.ToAgentId,
            MessageType = AcpMessageType.Request,
            Payload = JsonSerializer.Serialize(request.Payload),
            CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString(),
            ReplyTo = request.FromAgentId
        };

        await SendMessageAsync(message, ct);

        // Wait for response
        var tcs = new TaskCompletionSource<AcpMessage>();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout ?? TimeSpan.FromSeconds(30));

        // Manual async iteration to find matching response
        _ = Task.Run(async () =>
        {
            await foreach (var m in _transport.SubscribeAsync(request.FromAgentId, cts.Token))
            {
                if (m.CorrelationId == message.CorrelationId && m.MessageType == AcpMessageType.Response)
                {
                    tcs.TrySetResult(m);
                    break;
                }
            }
        }, cts.Token);

        try
        {
            var response = await tcs.Task;
            return new AcpResponse
            {
                Success = true,
                Payload = JsonSerializer.Deserialize<object>(response.Payload),
                Metadata = response.Metadata
            };
        }
        catch (OperationCanceledException)
        {
            return new AcpResponse
            {
                Success = false,
                Error = "Request timeout"
            };
        }
    }
}

// Data models

public sealed class AcpMessage
{
    public string MessageId { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public string FromAgentId { get; set; } = "";
    public string ToAgentId { get; set; } = "";
    public string ReplyTo { get; set; } = "";
    public AcpMessageType MessageType { get; set; }
    public string Payload { get; set; } = "";
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public int? TtlSeconds { get; set; }
}

public enum AcpMessageType
{
    Request,
    Response,
    Event,
    Command,
    Broadcast
}

public sealed class AcpRequest
{
    public string FromAgentId { get; set; } = "";
    public string ToAgentId { get; set; } = "";
    public object Payload { get; set; } = null!;
    public string? CorrelationId { get; set; }
}

public sealed class AcpResponse
{
    public bool Success { get; set; }
    public object? Payload { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

// Supporting interfaces

public interface IAcpTransport
{
    Task PublishAsync(AcpMessage message, CancellationToken ct);
    IAsyncEnumerable<AcpMessage> SubscribeAsync(string agentId, CancellationToken ct);
}

public interface IAcpMessageStore
{
    Task StoreAsync(AcpMessage message, CancellationToken ct);
    Task<IReadOnlyList<AcpMessage>> GetPendingAsync(string agentId, CancellationToken ct);
}
