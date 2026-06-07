using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public sealed class FileRunReviewStore : IRunReviewStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly AgentRuntimeOptions _options;
    private readonly ILogger<FileRunReviewStore> _logger;
    private readonly object _writeLock = new();

    public FileRunReviewStore(
        IOptions<AgentRuntimeOptions> options,
        ILogger<FileRunReviewStore> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string GetDecisionsPath(Guid runId) =>
        Path.Combine(ReviewDir(runId), "decisions.jsonl");

    public async Task AppendAsync(ReviewDecisionAuditEntry entry, CancellationToken ct = default)
    {
        var dir = ReviewDir(entry.RunId);
        Directory.CreateDirectory(dir);
        var path = GetDecisionsPath(entry.RunId);
        var line = JsonSerializer.Serialize(entry, JsonOptions);

        lock (_writeLock)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }

        _logger.LogInformation(
            "[RunReview] {Decision} path={Path} run={RunId}",
            entry.Decision,
            entry.Path,
            entry.RunId);

        await Task.CompletedTask;
    }

    public Task<IReadOnlyList<ReviewDecisionAuditEntry>> LoadAsync(Guid runId, CancellationToken ct = default)
    {
        _ = ct;
        var path = GetDecisionsPath(runId);
        if (!File.Exists(path))
            return Task.FromResult<IReadOnlyList<ReviewDecisionAuditEntry>>(Array.Empty<ReviewDecisionAuditEntry>());

        var entries = new List<ReviewDecisionAuditEntry>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var entry = JsonSerializer.Deserialize<ReviewDecisionAuditEntry>(line, JsonOptions);
                if (entry is not null)
                    entries.Add(entry);
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Skipping malformed review decision line for run {RunId}", runId);
            }
        }

        return Task.FromResult<IReadOnlyList<ReviewDecisionAuditEntry>>(entries);
    }

    private string ReviewDir(Guid runId) =>
        Path.Combine(Path.GetFullPath(_options.RunsRoot), runId.ToString("D"), "review");
}
