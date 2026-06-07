using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Skips incremental LLM tasks when targets already exist in workspace; collects phase artifacts.
/// </summary>
public static class IncrementalFileTaskPlanner
{
    public static (List<AgentTask> ToRun, List<AgentTask> Skipped) PartitionByExistingWorkspace(
        IReadOnlyList<AgentTask> tasks,
        IReadOnlyList<DomainGeneratedFile> workspace,
        AgentOrchestrationOptions options,
        PlannedFilePathRegistry? registry = null)
    {
        if (!options.SkipIncrementalTaskWhenTargetExists)
            return (tasks.ToList(), new List<AgentTask>());

        var minChars = Math.Clamp(options.MinCharsToSkipIncrementalTask, 20, 8_000);
        var byPath = workspace.ToDictionary(
            f => StackArtifactCompleteness.SanitizeRelativePath(f.RelativePath),
            f => f,
            StringComparer.OrdinalIgnoreCase);

        var toRun = new List<AgentTask>();
        var skipped = new List<AgentTask>();

        foreach (var task in tasks)
        {
            if (task.Context.TargetRelativePaths.Length == 0)
            {
                toRun.Add(task);
                continue;
            }

            var allComplete = task.Context.TargetRelativePaths.All(path =>
                CanSkipExistingTarget(path, byPath, minChars, registry, options));

            if (allComplete)
                skipped.Add(task);
            else
                toRun.Add(task);
        }

        return (toRun, skipped);
    }

    private static bool CanSkipExistingTarget(
        string targetPath,
        IReadOnlyDictionary<string, DomainGeneratedFile> byPath,
        int minChars,
        PlannedFilePathRegistry? registry,
        AgentOrchestrationOptions options)
    {
        if (options.UseExpandedJavaReactManifest
            && options.IncrementalSeedMode == IncrementalSeedMode.MinimalSpine
            && registry is not null
            && !registry.IsMinimalSpine(targetPath))
            return false;

        return IsTargetComplete(targetPath, byPath, minChars);
    }

    public static bool TryGetExistingCompleteTarget(
        string targetPath,
        IReadOnlyList<DomainGeneratedFile> workspace,
        int minChars,
        out DomainGeneratedFile? file)
    {
        file = null;
        var normalized = StackArtifactCompleteness.SanitizeRelativePath(targetPath);
        if (normalized.Length == 0)
            return false;

        var match = workspace.FirstOrDefault(f =>
            string.Equals(
                StackArtifactCompleteness.SanitizeRelativePath(f.RelativePath),
                normalized,
                StringComparison.OrdinalIgnoreCase));

        if (match is null || !IsContentComplete(match.Content, normalized, minChars))
            return false;

        file = match;
        return true;
    }

    public static List<DomainGeneratedFile> CollectPhaseWorkspaceFiles(
        AgentPhase phase,
        IReadOnlyList<DomainGeneratedFile> workspace,
        IEnumerable<string> additionalPaths)
    {
        var paths = new HashSet<string>(additionalPaths, StringComparer.OrdinalIgnoreCase);
        foreach (var file in workspace)
        {
            var path = StackArtifactCompleteness.SanitizeRelativePath(file.RelativePath);
            if (path.Length == 0)
                continue;

            if (BelongsToPhase(path, phase))
                paths.Add(path);
        }

        var byPath = workspace.ToDictionary(
            f => StackArtifactCompleteness.SanitizeRelativePath(f.RelativePath),
            f => f,
            StringComparer.OrdinalIgnoreCase);

        return paths
            .Where(byPath.ContainsKey)
            .Select(p => byPath[p])
            .ToList();
    }

    private static bool IsTargetComplete(
        string targetPath,
        IReadOnlyDictionary<string, DomainGeneratedFile> byPath,
        int minChars)
    {
        var normalized = StackArtifactCompleteness.SanitizeRelativePath(targetPath);
        return normalized.Length > 0
               && byPath.TryGetValue(normalized, out var file)
               && IsContentComplete(file.Content, normalized, minChars);
    }

    private static bool IsContentComplete(string? content, string path, int minChars)
    {
        var len = content?.Trim().Length ?? 0;
        if (len < minChars)
            return false;

        var threshold = ResolveMinCharsForPath(path, minChars);
        return len >= threshold;
    }

    private static int ResolveMinCharsForPath(string path, int baseline)
    {
        if (path.EndsWith("__init__.py", StringComparison.OrdinalIgnoreCase))
            return 1;

        if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            return Math.Min(baseline, 48);

        if (path.EndsWith(".java", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase))
            return Math.Max(baseline, 120);

        return baseline;
    }

    private static bool BelongsToPhase(string path, AgentPhase phase) =>
        phase switch
        {
            AgentPhase.Backend => path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase),
            AgentPhase.Frontend => path.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase),
            AgentPhase.Database => path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                                   && (path.Contains("/db/", StringComparison.OrdinalIgnoreCase)
                                       || path.Contains("/migration", StringComparison.OrdinalIgnoreCase)
                                       || path.Contains("/model/", StringComparison.OrdinalIgnoreCase)
                                       || path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
                                       || path.Contains("application.yml", StringComparison.OrdinalIgnoreCase)),
            _ => true
        };
}
