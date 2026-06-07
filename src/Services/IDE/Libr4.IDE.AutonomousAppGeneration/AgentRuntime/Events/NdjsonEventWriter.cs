using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;

public interface INdjsonEventWriter
{
    Task WriteAsync(Guid runId, object evt, CancellationToken ct = default);
}

public sealed class NdjsonEventWriter : INdjsonEventWriter
{
    private readonly AgentRuntimeOptions _options;
    private readonly IAgentRuntimeEventHub? _hub;
    private readonly object _lock = new();

    public NdjsonEventWriter(IOptions<AgentRuntimeOptions> options, IAgentRuntimeEventHub? hub = null)
    {
        _options = options.Value;
        _hub = hub;
    }

    public async Task WriteAsync(Guid runId, object evt, CancellationToken ct = default)
    {
        var path = Path.Combine(_options.RunsRoot, runId.ToString("D"), "events.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var line = JsonSerializer.Serialize(evt);
        lock (_lock)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }

        if (_hub is not null)
        {
            var eventType = TryGetEventType(line);
            await _hub.PublishAsync(
                new AgentRuntimePublishedEvent(runId, eventType, line, DateTimeOffset.UtcNow),
                ct).ConfigureAwait(false);
        }
    }

    private static string TryGetEventType(string jsonLine)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonLine);
            return doc.RootElement.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String
                ? t.GetString() ?? "unknown"
                : "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}
