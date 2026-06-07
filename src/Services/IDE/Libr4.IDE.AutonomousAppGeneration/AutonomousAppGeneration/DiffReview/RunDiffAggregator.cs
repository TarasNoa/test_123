using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public sealed class RunDiffAggregator : IRunDiffAggregator
{
    private static readonly HashSet<string> FileMutatingTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "write_file",
        "edit_file",
        "apply_patch"
    };

    private readonly AgentRuntimeOptions _options;
    private readonly IVerifyPassCheckpointService _checkpoints;
    private readonly ILogger<RunDiffAggregator> _logger;

    public RunDiffAggregator(
        IOptions<AgentRuntimeOptions> options,
        IVerifyPassCheckpointService checkpoints,
        ILogger<RunDiffAggregator> logger)
    {
        _options = options.Value;
        _checkpoints = checkpoints;
        _logger = logger;
    }

    public Task<RunDiffListResponse> ListAsync(Guid runId, RunDiffQuery query, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(query.CheckpointTag))
            return ListCheckpointAsync(runId, query, ct);

        var diffs = Aggregate(runId);
        var filtered = diffs.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.PathFilter))
        {
            filtered = filtered.Where(d =>
                d.Path.Contains(query.PathFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (query.StepNumber is int step)
            filtered = filtered.Where(d => d.StepNumber == step);

        var ordered = filtered
            .OrderByDescending(d => d.LastChangedUtc)
            .ThenBy(d => d.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var page = ordered
            .Skip(Math.Max(0, query.Offset))
            .Take(Math.Clamp(query.Limit, 1, 200))
            .Select(d => new RunFileDiffSummary(
                d.Path,
                d.Language,
                d.ChangeKind,
                d.StepNumber,
                d.ToolName,
                d.Hunks.Count,
                d.LastChangedUtc,
                d.ProvenanceId))
            .ToList();

        return Task.FromResult(new RunDiffListResponse(runId, ordered.Count, page));
    }

    public async Task<RunFileDiffDetail?> GetDetailAsync(
        Guid runId,
        string path,
        string? checkpointTag = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (!string.IsNullOrWhiteSpace(checkpointTag))
            return await GetCheckpointDetailAsync(runId, path, checkpointTag, ct).ConfigureAwait(false);

        var normalized = path.Replace('\\', '/').TrimStart('/');
        var diffs = Aggregate(runId);
        if (!diffs.TryGetValue(normalized, out var diff)
            && !diffs.TryGetValue(path, out diff))
        {
            diff = diffs.Values.FirstOrDefault(d =>
                d.Path.Equals(normalized, StringComparison.OrdinalIgnoreCase)
                || d.Path.EndsWith("/" + normalized, StringComparison.OrdinalIgnoreCase));
        }

        if (diff is null)
            return null;

        var unified = BuildUnifiedDiff(diff);
        var provenance = diff.Hunks
            .Select(h => new RunDiffProvenance(
                h.ProvenanceId,
                diff.StepNumber,
                diff.ToolName,
                true,
                null,
                diff.LastChangedUtc))
            .ToList();

        return new RunFileDiffDetail(
            runId,
            diff.Path,
            diff.Language,
            diff.ChangeKind,
            diff.Hunks,
            unified,
            provenance);
    }

    public async Task<RunDiffCheckpointListResponse> ListCheckpointsAsync(Guid runId, CancellationToken ct = default)
    {
        var checkpoints = await _checkpoints.ListCheckpointsAsync(runId, ct).ConfigureAwait(false);
        return new RunDiffCheckpointListResponse(runId, checkpoints);
    }

    private async Task<RunDiffListResponse> ListCheckpointAsync(
        Guid runId,
        RunDiffQuery query,
        CancellationToken ct)
    {
        var snapshot = await _checkpoints.LoadSnapshotAsync(runId, query.CheckpointTag!, ct).ConfigureAwait(false);
        if (snapshot is null)
            return new RunDiffListResponse(runId, 0, Array.Empty<RunFileDiffSummary>());

        var filtered = snapshot.Files.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query.PathFilter))
        {
            filtered = filtered.Where(d =>
                d.Path.Contains(query.PathFilter, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = filtered
            .OrderBy(d => d.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var page = ordered
            .Skip(Math.Max(0, query.Offset))
            .Take(Math.Clamp(query.Limit, 1, 200))
            .Select(d => new RunFileDiffSummary(
                d.Path,
                d.Language,
                d.ChangeKind,
                0,
                "verify_checkpoint",
                1,
                snapshot.TaggedAtUtc,
                $"checkpoint:{snapshot.Tag}"))
            .ToList();

        return new RunDiffListResponse(runId, ordered.Count, page);
    }

    private async Task<RunFileDiffDetail?> GetCheckpointDetailAsync(
        Guid runId,
        string path,
        string checkpointTag,
        CancellationToken ct)
    {
        var snapshot = await _checkpoints.LoadSnapshotAsync(runId, checkpointTag, ct).ConfigureAwait(false);
        if (snapshot is null)
            return null;

        var normalized = path.Replace('\\', '/').TrimStart('/');
        var file = snapshot.Files.FirstOrDefault(f =>
            f.Path.Equals(normalized, StringComparison.OrdinalIgnoreCase)
            || f.Path.EndsWith("/" + normalized, StringComparison.OrdinalIgnoreCase));

        if (file is null)
            return null;

        var hunk = new RunDiffHunk(1, 1, file.UnifiedDiff, Truncate(file.UnifiedDiff, 1200), $"checkpoint:{snapshot.Tag}");
        var provenance = new[]
        {
            new RunDiffProvenance(
                $"checkpoint:{snapshot.Tag}",
                0,
                "verify_checkpoint",
                true,
                null,
                snapshot.TaggedAtUtc)
        };

        return new RunFileDiffDetail(
            runId,
            file.Path,
            file.Language,
            file.ChangeKind,
            new[] { hunk },
            file.UnifiedDiff,
            provenance);
    }

    private Dictionary<string, RunFileDiff> Aggregate(Guid runId)
    {
        var runDir = RunDir(runId);
        var map = new Dictionary<string, RunFileDiff>(StringComparer.OrdinalIgnoreCase);

        ReadRollout(runDir, map);
        ReadPatchAttempts(runDir, map);

        return map;
    }

    private void ReadRollout(string runDir, Dictionary<string, RunFileDiff> map)
    {
        var path = Path.Combine(runDir, "rollout.jsonl");
        if (!File.Exists(path))
            return;

        var lineNo = 0;
        foreach (var line in File.ReadLines(path))
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "tool_use")
                    continue;

                var toolName = root.TryGetProperty("toolName", out var toolEl) ? toolEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(toolName) || !FileMutatingTools.Contains(toolName))
                    continue;

                var stepNumber = root.TryGetProperty("stepNumber", out var stepEl) ? stepEl.GetInt32() : 0;
                var success = !root.TryGetProperty("success", out var successEl) || successEl.GetBoolean();
                if (!success)
                    continue;

                var inputJson = root.TryGetProperty("inputJson", out var inputEl) ? inputEl.GetString() : null;
                var outputJson = root.TryGetProperty("outputJson", out var outputEl) ? outputEl.GetString() : null;
                var filePath = ExtractPath(inputJson);
                if (string.IsNullOrWhiteSpace(filePath))
                    continue;

                filePath = NormalizePath(filePath);
                var timestamp = ReadTimestamp(root);
                var provenanceId = $"rollout:{lineNo}";
                var kind = InferChangeKind(toolName, outputJson);
                var snippet = Truncate(outputJson ?? inputJson, 1200);
                var hunk = new RunDiffHunk(1, 1, null, snippet, provenanceId);

                if (map.TryGetValue(filePath, out var existing))
                {
                    var hunks = existing.Hunks.Concat(new[] { hunk }).ToList();
                    map[filePath] = existing with
                    {
                        ChangeKind = MergeKind(existing.ChangeKind, kind),
                        StepNumber = stepNumber,
                        ToolName = toolName,
                        Hunks = hunks,
                        LastChangedUtc = timestamp,
                        ProvenanceId = provenanceId
                    };
                }
                else
                {
                    map[filePath] = new RunFileDiff(
                        filePath,
                        InferLanguage(filePath),
                        kind,
                        stepNumber,
                        toolName,
                        null,
                        new[] { hunk },
                        timestamp,
                        provenanceId);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Skipping malformed rollout line {Line}", lineNo);
            }
        }
    }

    private void ReadPatchAttempts(string runDir, Dictionary<string, RunFileDiff> map)
    {
        var patchesDir = Path.Combine(runDir, "patches");
        if (!Directory.Exists(patchesDir))
            return;

        foreach (var file in Directory.EnumerateFiles(patchesDir, "*.json"))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var root = doc.RootElement;
                var path = root.TryGetProperty("path", out var pathEl) ? pathEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                path = NormalizePath(path);
                var success = !root.TryGetProperty("success", out var successEl) || successEl.GetBoolean();
                if (!success)
                    continue;

                var patch = root.TryGetProperty("patch", out var patchEl) ? patchEl.GetString() : null;
                var timestamp = root.TryGetProperty("timestamp", out var tsEl) && tsEl.ValueKind == JsonValueKind.String
                    && DateTime.TryParse(tsEl.GetString(), out var parsed)
                    ? parsed
                    : File.GetLastWriteTimeUtc(file);

                var provenanceId = $"patch:{Path.GetFileName(file)}";
                var hunk = new RunDiffHunk(1, 1, patch, Truncate(patch, 1200), provenanceId);

                if (map.TryGetValue(path, out var existing))
                {
                    map[path] = existing with
                    {
                        Hunks = existing.Hunks.Concat(new[] { hunk }).ToList(),
                        LastChangedUtc = timestamp,
                        ProvenanceId = provenanceId
                    };
                }
                else
                {
                    map[path] = new RunFileDiff(
                        path,
                        InferLanguage(path),
                        RunDiffChangeKind.Modify,
                        0,
                        "apply_patch",
                        null,
                        new[] { hunk },
                        timestamp,
                        provenanceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Skipping patch file {File}", file);
            }
        }
    }

    private string RunDir(Guid runId) =>
        Path.Combine(Path.GetFullPath(_options.RunsRoot), runId.ToString("D"));

    private static string? ExtractPath(string? inputJson)
    {
        if (string.IsNullOrWhiteSpace(inputJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(inputJson);
            if (doc.RootElement.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
                return pathEl.GetString();
        }
        catch (JsonException)
        {
            // ignore
        }

        return null;
    }

    private static RunDiffChangeKind InferChangeKind(string toolName, string? outputJson)
    {
        if (toolName.Equals("write_file", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(outputJson)
                && outputJson.Contains("wrote", StringComparison.OrdinalIgnoreCase))
                return RunDiffChangeKind.Add;
        }

        return RunDiffChangeKind.Modify;
    }

    private static RunDiffChangeKind MergeKind(RunDiffChangeKind existing, RunDiffChangeKind incoming) =>
        existing == RunDiffChangeKind.Add && incoming == RunDiffChangeKind.Modify
            ? RunDiffChangeKind.Modify
            : incoming;

    private static DateTime ReadTimestamp(JsonElement root)
    {
        if (root.TryGetProperty("timestamp", out var ts) && ts.TryGetInt64(out var ms))
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;

        return DateTime.UtcNow;
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');

    private static string InferLanguage(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "cs" => "csharp",
            "ts" or "tsx" => "typescript",
            "js" or "jsx" => "javascript",
            "py" => "python",
            "json" => "json",
            "yaml" or "yml" => "yaml",
            "md" => "markdown",
            _ => ext.Length > 0 ? ext : "text"
        };
    }

    private static string? BuildUnifiedDiff(RunFileDiff diff)
    {
        var patch = diff.Hunks
            .Select(h => h.UnifiedDiff ?? h.Snippet)
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        return patch;
    }

    private static string? Truncate(string? text, int max)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= max)
            return text;
        return text[..max] + "\n...[truncated]";
    }
}
