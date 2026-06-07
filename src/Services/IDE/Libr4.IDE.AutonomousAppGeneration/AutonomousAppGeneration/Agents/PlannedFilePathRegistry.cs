using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Tracks the deterministic manifest, rejects stray paths, and reports coverage.
/// </summary>
public sealed class PlannedFilePathRegistry
{
    private readonly HashSet<string> _allowed;
    private readonly Dictionary<string, PlannedFileEntry> _byPath;

    public PlannedFilePathRegistry(IReadOnlyList<PlannedFileEntry> entries)
    {
        _byPath = entries.ToDictionary(
            e => Normalize(e.Path),
            e => e,
            StringComparer.OrdinalIgnoreCase);
        _allowed = new HashSet<string>(_byPath.Keys, StringComparer.OrdinalIgnoreCase);
    }

    public int PlannedCount => _allowed.Count;

    public IReadOnlySet<string> AllowedPaths => _allowed;

    public IReadOnlyList<string> PathsForPhase(AgentPhase phase) =>
        EntriesForPhase(phase).Select(e => e.Path).ToList();

    public IReadOnlyList<PlannedFileEntry> EntriesForPhase(AgentPhase phase) =>
        _byPath.Values
            .Where(e => e.Phase == phase)
            .OrderBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public bool IsAllowed(string? path) =>
        !string.IsNullOrWhiteSpace(path) && _allowed.Contains(Normalize(path));

    public bool IsMinimalSpine(string? path) =>
        JavaReactExpandedFileManifest.MinimalSpinePaths.Contains(Normalize(path));

    public IReadOnlyList<DomainGeneratedFile> AcceptOnlyPlanned(
        IReadOnlyList<DomainGeneratedFile> parsed,
        IReadOnlyList<string> targetPaths)
    {
        var allowedTargets = targetPaths
            .Select(Normalize)
            .Where(p => p.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new List<DomainGeneratedFile>();
        foreach (var file in parsed)
        {
            var path = Normalize(file.RelativePath);
            if (path.Length == 0)
                continue;

            if (!IsAllowed(path))
                continue;

            if (allowedTargets.Count > 0 && !allowedTargets.Contains(path))
                continue;

            result.Add(new DomainGeneratedFile(path, file.Language, file.Content));
        }

        return result;
    }

    public PlannedFileCoverageReport EvaluateCoverage(IReadOnlyList<DomainGeneratedFile> workspace)
    {
        var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in workspace)
        {
            var path = Normalize(file.RelativePath);
            if (path.Length > 0 && _allowed.Contains(path)
                && !string.IsNullOrWhiteSpace(file.Content))
                present.Add(path);
        }

        var missing = _allowed.Where(p => !present.Contains(p)).OrderBy(p => p).ToList();
        var extra = workspace
            .Select(f => Normalize(f.RelativePath))
            .Where(p => p.Length > 0 && !_allowed.Contains(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p)
            .ToList();

        var coverage = _allowed.Count == 0
            ? 1.0
            : (double)present.Count / _allowed.Count;

        return new PlannedFileCoverageReport(
            Planned: _allowed.Count,
            Present: present.Count,
            Missing: missing,
            Extra: extra,
            CoverageRatio: coverage);
    }

    private static string Normalize(string? path) =>
        StackArtifactCompleteness.SanitizeRelativePath(path ?? string.Empty);
}

public sealed record PlannedFileCoverageReport(
    int Planned,
    int Present,
    IReadOnlyList<string> Missing,
    IReadOnlyList<string> Extra,
    double CoverageRatio);
