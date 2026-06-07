using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public sealed class FileSpaceContextBus : ISpaceContextBus
{
    private readonly AgentSpaceOptions _options;
    private readonly ISpaceContextFanout? _fanout;
    private readonly ILogger<FileSpaceContextBus>? _logger;
    private readonly object _lock = new();

    public FileSpaceContextBus(
        IOptions<AgentSpaceOptions> options,
        ISpaceContextFanout? fanout = null,
        ILogger<FileSpaceContextBus>? logger = null)
    {
        _options = options.Value;
        _fanout = fanout;
        _logger = logger;
    }

    public string BuildHermesScope(Guid spaceId) => $"project:{spaceId:D}";

    public string BuildDMailAddress(Guid spaceId, SpaceMemberRole role) =>
        $"@space/{spaceId:D}/{role.ToString().ToLowerInvariant()}";

    public async Task PublishAsync(
        Guid spaceId,
        string kind,
        string title,
        string? payload,
        string? authorMemberId = null,
        CancellationToken ct = default)
    {
        var evt = new SpaceContextEvent(
            EventId: Guid.NewGuid().ToString("N")[..12],
            SpaceId: spaceId,
            Kind: kind,
            Title: title,
            Payload: payload,
            AuthorMemberId: authorMemberId,
            TimestampUtc: DateTime.UtcNow);

        var sharedDir = SharedDir(spaceId);
        Directory.CreateDirectory(sharedDir);
        var eventsPath = EventsPath(spaceId);
        var line = JsonSerializer.Serialize(evt) + Environment.NewLine;

        lock (_lock)
        {
            File.AppendAllText(eventsPath, line);
        }

        await WriteSharedSnapshotAsync(sharedDir, kind, title, payload, ct).ConfigureAwait(false);

        if (_fanout is not null)
        {
            try
            {
                await _fanout.FanoutAsync(evt, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Space context fanout failed for {SpaceId}", spaceId);
            }
        }
    }

    public Task<IReadOnlyList<SpaceContextEvent>> ReadRecentAsync(Guid spaceId, int limit = 32, CancellationToken ct = default)
    {
        var path = EventsPath(spaceId);
        if (!File.Exists(path))
            return Task.FromResult<IReadOnlyList<SpaceContextEvent>>(Array.Empty<SpaceContextEvent>());

        var lines = File.ReadLines(path).ToList();
        var events = new List<SpaceContextEvent>(Math.Min(limit, lines.Count));
        for (var i = Math.Max(0, lines.Count - limit); i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;
            try
            {
                var evt = JsonSerializer.Deserialize<SpaceContextEvent>(lines[i]);
                if (evt is not null)
                    events.Add(evt);
            }
            catch (JsonException)
            {
                // skip malformed line
            }
        }

        return Task.FromResult<IReadOnlyList<SpaceContextEvent>>(events);
    }

    private string SpaceRoot(Guid spaceId) =>
        Path.Combine(Path.GetFullPath(_options.SpacesRoot), spaceId.ToString("D"));

    private string SharedDir(Guid spaceId) => Path.Combine(SpaceRoot(spaceId), "shared");

    private string EventsPath(Guid spaceId) => Path.Combine(SharedDir(spaceId), "context-events.jsonl");

    private static async Task WriteSharedSnapshotAsync(string sharedDir, string kind, string title, string? payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return;

        var fileName = kind.ToLowerInvariant() switch
        {
            "plan" or "plan_summary" => "plan.md",
            "design" => "design.md",
            "api" or "openapi" => "api-openapi.yaml",
            "verify" => "verify-summary.md",
            _ => $"{kind.ToLowerInvariant()}.md"
        };

        var path = Path.Combine(sharedDir, fileName);
        var content = $"# {title}\n\n{payload}\n";
        await File.WriteAllTextAsync(path, content, ct).ConfigureAwait(false);
    }
}
