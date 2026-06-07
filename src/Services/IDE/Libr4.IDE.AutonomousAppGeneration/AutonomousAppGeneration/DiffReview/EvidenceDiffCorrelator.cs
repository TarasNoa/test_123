using System.Text.Json;
using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public sealed class EvidenceDiffCorrelator : IEvidenceDiffCorrelator
{
    private static readonly Regex StackPathPattern = new(
        @"(?<path>(?:[\w.-]+/)+[\w.-]+\.(?:tsx?|jsx?|vue|py|cs|java|go|rs|php|rb|swift|kt|scala|css|scss|html|json|yaml|yml|md))(?:\:(?<line>\d+))?(?:\:(?<col>\d+))?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromMilliseconds(250));

    private readonly IRunDiffAggregator _diffs;
    private readonly IObscuraEvidenceStore? _obscura;
    private readonly IVerifyEvidenceStore? _verify;
    private readonly IAppGenerationRepository? _repository;
    private readonly ILogger<EvidenceDiffCorrelator> _logger;

    public EvidenceDiffCorrelator(
        IRunDiffAggregator diffs,
        ILogger<EvidenceDiffCorrelator> logger,
        IObscuraEvidenceStore? obscura = null,
        IVerifyEvidenceStore? verify = null,
        IAppGenerationRepository? repository = null)
    {
        _diffs = diffs;
        _logger = logger;
        _obscura = obscura;
        _verify = verify;
        _repository = repository;
    }

    public async Task<FileDiffEvidenceResponse?> GetForPathAsync(
        Guid runId,
        string path,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var normalized = NormalizePath(path);
        var detail = await _diffs.GetDetailAsync(runId, normalized, checkpointTag: null, ct).ConfigureAwait(false);
        if (detail is null)
            return null;

        var stepNumber = detail.Provenance
            .OrderByDescending(p => p.StepNumber)
            .Select(p => (int?)p.StepNumber)
            .FirstOrDefault(s => s > 0);

        var items = new List<DiffEvidenceItem>();
        items.AddRange(CollectObscuraItems(runId, stepNumber));
        items.AddRange(CollectVerifyItems(runId));

        var overlayIndex = await GetOverlaysAsync(runId, ct).ConfigureAwait(false);
        var overlays = overlayIndex.Paths
            .Where(p => PathMatches(p.Path, normalized))
            .SelectMany(p => p.OverlayKinds.Zip(p.Reasons, (kind, reason) =>
                new DiffEvidenceOverlay(kind, reason, ExtractCategory(reason))))
            .DistinctBy(o => $"{o.Kind}:{o.Reason}")
            .ToList();

        return new FileDiffEvidenceResponse(
            runId,
            detail.Path,
            stepNumber,
            items,
            overlays);
    }

    public async Task<DiffEvidenceOverlayIndex> GetOverlaysAsync(Guid runId, CancellationToken ct = default)
    {
        _ = ct;
        var map = new Dictionary<string, (HashSet<string> Kinds, List<string> Reasons)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var path in ExtractConsoleErrorPaths(runId))
            AddOverlay(map, path, "verify_console", $"console_error:{path}");

        foreach (var (path, reason) in await LoadSecurityFlaggedPathsAsync(runId).ConfigureAwait(false))
            AddOverlay(map, path, "security_flag", reason);

        var paths = map
            .Select(kvp => new DiffPathOverlay(
                kvp.Key,
                kvp.Value.Kinds.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList(),
                kvp.Value.Reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToList()))
            .OrderBy(p => p.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DiffEvidenceOverlayIndex(runId, paths);
    }

    private IReadOnlyList<DiffEvidenceItem> CollectObscuraItems(Guid runId, int? stepNumber)
    {
        if (_obscura is null)
            return Array.Empty<DiffEvidenceItem>();

        try
        {
            return _obscura.List(runId).Artifacts
                .Where(a => stepNumber is null || a.StepNumber == stepNumber)
                .Select(a => new DiffEvidenceItem(
                    "obscura",
                    a.Kind.ToString(),
                    a.FileName,
                    a.DownloadUrl,
                    a.ThumbnailUrl,
                    a.StepNumber,
                    a.ToolName,
                    stepNumber is int s && a.StepNumber == s,
                    a.SizeBytes,
                    a.LastModifiedUtc))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to list Obscura evidence for run {RunId}", runId);
            return Array.Empty<DiffEvidenceItem>();
        }
    }

    private IReadOnlyList<DiffEvidenceItem> CollectVerifyItems(Guid runId)
    {
        if (_verify is null)
            return Array.Empty<DiffEvidenceItem>();

        try
        {
            return _verify.List(runId).Artifacts
                .Select(a => new DiffEvidenceItem(
                    "verify",
                    a.Kind.ToString(),
                    a.FileName,
                    a.DownloadUrl,
                    a.ThumbnailUrl,
                    null,
                    null,
                    false,
                    a.SizeBytes,
                    a.LastModifiedUtc))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to list verify evidence for run {RunId}", runId);
            return Array.Empty<DiffEvidenceItem>();
        }
    }

    private IReadOnlyList<string> ExtractConsoleErrorPaths(Guid runId)
    {
        if (_verify is null)
            return Array.Empty<string>();

        var artifact = _verify.TryGet(runId, "console-errors.json");
        if (artifact is null || !File.Exists(artifact.AbsolutePath))
            return Array.Empty<string>();

        try
        {
            var json = File.ReadAllText(artifact.AbsolutePath);
            return ParseConsoleErrorPaths(json);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse console-errors.json for run {RunId}", runId);
            return Array.Empty<string>();
        }
    }

    public static IReadOnlyList<string> ParseConsoleErrorPaths(string json)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                    CollectPathsFromElement(item, paths);
            }
            else
            {
                CollectPathsFromElement(doc.RootElement, paths);
            }
        }
        catch (JsonException)
        {
            foreach (Match match in StackPathPattern.Matches(json))
                paths.Add(NormalizePath(match.Groups["path"].Value));
        }

        return paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void CollectPathsFromElement(JsonElement item, HashSet<string> paths)
    {
        foreach (var property in new[] { "message", "stack", "source", "text", "url" })
        {
            if (item.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String)
                CollectPathsFromText(el.GetString(), paths);
        }

        if (item.TryGetProperty("file", out var fileEl) && fileEl.ValueKind == JsonValueKind.String)
        {
            var file = NormalizePath(fileEl.GetString()!);
            if (!string.IsNullOrWhiteSpace(file))
                paths.Add(file);
        }
    }

    private static void CollectPathsFromText(string? text, HashSet<string> paths)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        foreach (Match match in StackPathPattern.Matches(text))
            paths.Add(NormalizePath(match.Groups["path"].Value));
    }

    private async Task<IReadOnlyList<(string Path, string Reason)>> LoadSecurityFlaggedPathsAsync(Guid runId)
    {
        var results = new List<(string Path, string Reason)>();
        if (_repository is null)
            return results;

        try
        {
            var orchestrator = await _repository.GetAsync(runId).ConfigureAwait(false);
            if (orchestrator is null)
                return results;

            foreach (var review in orchestrator.SecurityReviews.Where(r => !r.Passed))
            {
                foreach (var reason in review.Reasons)
                {
                    if (TryExtractPathFromSecurityReason(reason, out var path))
                        results.Add((path, reason));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load security reviews for run {RunId}", runId);
        }

        return results;
    }

    public static bool TryExtractPathFromSecurityReason(string reason, out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(reason))
            return false;

        var segments = reason.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2)
            return false;

        path = NormalizePath(segments[^1]);
        return path.Contains('/') || path.Contains('.') || path.Contains('\\');
    }

    private static void AddOverlay(
        Dictionary<string, (HashSet<string> Kinds, List<string> Reasons)> map,
        string path,
        string kind,
        string reason)
    {
        path = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!map.TryGetValue(path, out var entry))
        {
            entry = (new HashSet<string>(StringComparer.OrdinalIgnoreCase), new List<string>());
            map[path] = entry;
        }

        entry.Kinds.Add(kind);
        entry.Reasons.Add(reason);
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');

    private static bool PathMatches(string candidate, string target)
    {
        candidate = NormalizePath(candidate);
        target = NormalizePath(target);
        return candidate.Equals(target, StringComparison.OrdinalIgnoreCase)
               || candidate.EndsWith('/' + target, StringComparison.OrdinalIgnoreCase)
               || target.EndsWith('/' + candidate, StringComparison.OrdinalIgnoreCase)
               || string.Equals(Path.GetFileName(candidate), Path.GetFileName(target), StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractCategory(string reason)
    {
        var idx = reason.IndexOf(':');
        return idx > 0 ? reason[..idx] : null;
    }
}
