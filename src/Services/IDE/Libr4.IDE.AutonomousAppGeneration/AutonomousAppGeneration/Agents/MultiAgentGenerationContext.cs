using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Injects workspace snapshots into agent tasks so each LLM call sees what was already written.
/// </summary>
public static class MultiAgentGenerationContext
{
    public static void ApplyWorkspaceSnapshot(
        AgentTask task,
        IReadOnlyList<DomainGeneratedFile> workspace,
        AgentOrchestrationOptions options)
    {
        var targets = task.Context.TargetRelativePaths;
        if (targets.Length == 0 && !task.Context.ScopedOutputOnly)
            return;

        var selected = SelectFilesForPrompt(workspace, targets, options);
        task.Context.GeneratedFiles = selected
            .Select(f => new GeneratedFile
            {
                RelativePath = f.RelativePath,
                Content = TruncateContent(f.Content, options.MaxExistingFileContentChars)
            })
            .ToArray();
    }

    public static IReadOnlyList<DomainGeneratedFile> FilterParsedToTargets(
        IReadOnlyList<DomainGeneratedFile> parsed,
        IReadOnlyList<string> targetPaths)
    {
        if (targetPaths.Count == 0)
            return parsed;

        var allowed = new HashSet<string>(
            targetPaths.Select(StackArtifactCompleteness.SanitizeRelativePath).Where(p => p.Length > 0),
            StringComparer.OrdinalIgnoreCase);

        return parsed
            .Select(f =>
            {
                var path = StackArtifactCompleteness.SanitizeRelativePath(f.RelativePath);
                return string.IsNullOrWhiteSpace(path)
                    ? null
                    : new DomainGeneratedFile(path, f.Language, f.Content);
            })
            .Where(f => f is not null && allowed.Contains(f!.RelativePath))
            .Cast<DomainGeneratedFile>()
            .ToList();
    }

    private static List<DomainGeneratedFile> SelectFilesForPrompt(
        IReadOnlyList<DomainGeneratedFile> workspace,
        IReadOnlyList<string> targetPaths,
        AgentOrchestrationOptions options)
    {
        var maxFiles = Math.Clamp(options.MaxExistingFilesInPrompt, 4, 40);
        if (workspace.Count == 0)
            return new List<DomainGeneratedFile>();

        if (targetPaths.Count == 0)
        {
            return workspace
                .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Take(maxFiles)
                .ToList();
        }

        var targets = targetPaths
            .Select(StackArtifactCompleteness.SanitizeRelativePath)
            .Where(p => p.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sameDir = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in targets)
        {
            var dir = GetDirectoryPrefix(t);
            if (dir.Length > 0)
                sameDir.Add(dir);
        }

        var ranked = workspace
            .Select(f => (File: f, Score: ScoreFile(f.RelativePath, targets, sameDir)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.File.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Take(maxFiles)
            .Select(x => x.File)
            .ToList();

        if (ranked.Count == 0)
        {
            return workspace
                .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Min(8, maxFiles))
                .ToList();
        }

        return ranked;
    }

    private static int ScoreFile(string path, HashSet<string> targets, HashSet<string> sameDirPrefixes)
    {
        if (targets.Contains(path))
            return 100;

        foreach (var prefix in sameDirPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return 50;
        }

        if (path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase))
            return 10;

        return 0;
    }

    private static string GetDirectoryPrefix(string path)
    {
        var idx = path.LastIndexOf('/');
        return idx <= 0 ? string.Empty : path[..(idx + 1)];
    }

    private static string TruncateContent(string? content, int maxChars)
    {
        if (string.IsNullOrEmpty(content) || maxChars <= 0)
            return string.Empty;

        if (content.Length <= maxChars)
            return content;

        return content[..maxChars] + "\n... (truncated for context budget)";
    }
}
