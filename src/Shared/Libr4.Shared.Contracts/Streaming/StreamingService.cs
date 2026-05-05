using System.Threading.Channels;

namespace Libr4.Shared.Contracts.Streaming;

/// <summary>
/// Sandbox template for code generation
/// </summary>
public class SandboxTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Port { get; set; }
}

/// <summary>
/// Streaming event type.
/// </summary>
public enum StreamingEventType
{
    /// <summary>
    /// Initial event with metadata.
    /// </summary>
    Start,

    /// <summary>
    /// Partial content update.
    /// </summary>
    Content,

    /// <summary>
    /// Schema field update.
    /// </summary>
    Field,

    /// <summary>
    /// Error event.
    /// </summary>
    Error,

    /// <summary>
    /// Completion event.
    /// </summary>
    Done
}

/// <summary>
/// Streaming event.
/// </summary>
public record StreamingEvent
{
    /// <summary>
    /// Event type.
    /// </summary>
    public StreamingEventType Type { get; init; }

    /// <summary>
    /// Event data.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Field name (for field updates).
    /// </summary>
    public string? Field { get; init; }

    /// <summary>
    /// Timestamp.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Session ID.
    /// </summary>
    public string SessionId { get; init; } = string.Empty;
}

/// <summary>
/// Streaming session.
/// </summary>
public record StreamingSession
{
    /// <summary>
    /// Unique session ID.
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>
    /// User ID.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Session start time.
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the session is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Session metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Streaming service interface.
/// </summary>
public interface IStreamingService
{
    /// <summary>
    /// Creates a new streaming session.
    /// </summary>
    /// <param name="userId">User ID (optional).</param>
    /// <param name="metadata">Session metadata.</param>
    /// <returns>Streaming session.</returns>
    Task<StreamingSession> CreateSessionAsync(
        string? userId = null,
        Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Streaming session or null.</returns>
    Task<StreamingSession?> GetSessionAsync(string sessionId);

    /// <summary>
    /// Ends a session.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    Task EndSessionAsync(string sessionId);

    /// <summary>
    /// Streams events to a session.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of events.</returns>
    IAsyncEnumerable<StreamingEvent> StreamEventsAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an event to a session.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="data">Event data.</param>
    /// <param name="field">Field name (optional).</param>
    Task SendEventAsync(
        string sessionId,
        StreamingEventType eventType,
        object? data = null,
        string? field = null);
}

/// <summary>
/// In-memory streaming service for development and testing.
/// </summary>
public class InMemoryStreamingService : IStreamingService
{
    private readonly Dictionary<string, StreamingSession> _sessions = new();
    private readonly Dictionary<string, Channel<StreamingEvent>> _channels = new();
    private readonly object _lock = new();

    public async Task<StreamingSession> CreateSessionAsync(
        string? userId = null,
        Dictionary<string, string>? metadata = null)
    {
        await Task.CompletedTask;

        var sessionId = Guid.NewGuid().ToString();
        var session = new StreamingSession
        {
            SessionId = sessionId,
            UserId = userId,
            StartTime = DateTime.UtcNow,
            IsActive = true,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        lock (_lock)
        {
            _sessions[sessionId] = session;
            _channels[sessionId] = Channel.CreateUnbounded<StreamingEvent>();
        }

        return session;
    }

    public async Task<StreamingSession?> GetSessionAsync(string sessionId)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }
    }

    public async Task EndSessionAsync(string sessionId)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session = session with { IsActive = false };
                _sessions[sessionId] = session;
            }

            if (_channels.TryGetValue(sessionId, out var channel))
            {
                channel.Writer.Complete();
                _channels.Remove(sessionId);
            }
        }
    }

    public async IAsyncEnumerable<StreamingEvent> StreamEventsAsync(
        string sessionId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Channel<StreamingEvent>? channel;

        lock (_lock)
        {
            _channels.TryGetValue(sessionId, out channel);
        }

        if (channel == null)
        {
            yield break;
        }

        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }
    }

    public async Task SendEventAsync(
        string sessionId,
        StreamingEventType eventType,
        object? data = null,
        string? field = null)
    {
        await Task.CompletedTask;

        Channel<StreamingEvent>? channel;

        lock (_lock)
        {
            _channels.TryGetValue(sessionId, out channel);
        }

        if (channel == null)
        {
            return;
        }

        var evt = new StreamingEvent
        {
            Type = eventType,
            Data = data,
            Field = field,
            SessionId = sessionId
        };

        await channel.Writer.WriteAsync(evt);
    }
}

/// <summary>
/// Streaming response formatter for Server-Sent Events (SSE).
/// </summary>
public static class StreamingFormatter
{
    /// <summary>
    /// Formats a streaming event as SSE.
    /// </summary>
    public static string FormatAsSSE(StreamingEvent evt)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.Append($"event: {evt.Type.ToString().ToLowerInvariant()}\n");
        sb.Append($"id: {evt.SessionId}\n");
        sb.Append($"time: {evt.Timestamp:O}\n");

        if (!string.IsNullOrEmpty(evt.Field))
        {
            sb.Append($"field: {evt.Field}\n");
        }

        if (evt.Data != null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(evt.Data);
            sb.Append($"data: {json}\n");
        }

        sb.Append("\n");

        return sb.ToString();
    }

    /// <summary>
    /// Formats a streaming event as JSON.
    /// </summary>
    public static string FormatAsJson(StreamingEvent evt)
    {
        return System.Text.Json.JsonSerializer.Serialize(evt);
    }
}

/// <summary>
/// Streaming code generation service.
/// </summary>
public class StreamingCodeGenerationService
{
    private readonly IStreamingService _streamingService;

    public StreamingCodeGenerationService(IStreamingService streamingService)
    {
        _streamingService = streamingService;
    }

    /// <summary>
    /// Generates code with streaming output.
    /// </summary>
    /// <param name="prompt">User prompt.</param>
    /// <param name="template">Sandbox template.</param>
    /// <param name="userId">User ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session ID for streaming.</returns>
    public async Task<string> GenerateCodeWithStreamingAsync(
        string prompt,
        SandboxTemplate template,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _streamingService.CreateSessionAsync(userId, new Dictionary<string, string>
        {
            ["template"] = template.Id,
            ["language"] = template.Language
        });

        // Start generation in background
        _ = Task.Run(async () => await GenerateCodeInternalAsync(session.SessionId, prompt, template, cancellationToken));

        return session.SessionId;
    }

    private async Task GenerateCodeInternalAsync(
        string sessionId,
        string prompt,
        SandboxTemplate template,
        CancellationToken cancellationToken)
    {
        try
        {
            // Send start event
            await _streamingService.SendEventAsync(sessionId, StreamingEventType.Start, new
            {
                template = template.Id,
                language = template.Language
            });

            // Simulate streaming generation (in real implementation, this would call LLM)
            await Task.Delay(500, cancellationToken);

            // Send commentary
            await _streamingService.SendEventAsync(sessionId, StreamingEventType.Field, 
                "Analyzing requirements and selecting appropriate template...", 
                "commentary");

            await Task.Delay(500, cancellationToken);

            // Send title
            await _streamingService.SendEventAsync(sessionId, StreamingEventType.Field,
                "Sample App",
                "title");

            await Task.Delay(300, cancellationToken);

            // Send description
            await _streamingService.SendEventAsync(sessionId, StreamingEventType.Field,
                "A simple application generated from the prompt.",
                "description");

            await Task.Delay(300, cancellationToken);

            // Send code in chunks
            var code = GenerateSampleCode(template);
            var chunks = SplitIntoChunks(code, 100);

            foreach (var chunk in chunks)
            {
                await _streamingService.SendEventAsync(sessionId, StreamingEventType.Content, chunk);
                await Task.Delay(100, cancellationToken);
            }

            // Send done event
            await _streamingService.SendEventAsync(sessionId, StreamingEventType.Done, new
            {
                success = true,
                codeLength = code.Length
            });
        }
        catch (OperationCanceledException)
        {
            await _streamingService.SendEventAsync(sessionId, StreamingEventType.Done, new
            {
                success = false,
                cancelled = true
            });
        }
        catch (Exception ex)
        {
            await _streamingService.SendEventAsync(sessionId, StreamingEventType.Error, new
            {
                message = ex.Message
            });
        }
        finally
        {
            await _streamingService.EndSessionAsync(sessionId);
        }
    }

    private string GenerateSampleCode(SandboxTemplate template)
    {
        return template.Language switch
        {
            "python" => "import streamlit as st\n\nst.title('Hello World')\nst.write('This is a generated app')",
            "typescript" => "import React from 'react';\n\nexport default function App() {\n  return <div>Hello World</div>;\n}",
            "csharp" => "var app = WebApplication.CreateBuilder(args).Build();\napp.MapGet(\"/\", () => \"Hello World\");\napp.Run();",
            _ => "// Generated code"
        };
    }

    private List<string> SplitIntoChunks(string text, int chunkSize)
    {
        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
        }
        return chunks;
    }
}
