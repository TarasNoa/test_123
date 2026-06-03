using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Removes toolchain artifacts that contradict the planned tech stack (e.g. stray .NET files when the plan is Python-only).
/// </summary>
internal static class TechStackArtifactFilter
{
    public static IReadOnlyList<GenerationPhaseBatchResult> PrunePhaseBatches(
        IReadOnlyList<GenerationPhaseBatchResult> batches,
        GenerationPlan plan)
    {
        if (!ShouldDropDotNetArtifacts(plan))
            return batches;

        return batches
            .Select(b => new GenerationPhaseBatchResult(b.PhaseName, PruneFiles(b.Files, plan)))
            .ToList();
    }

    public static IReadOnlyList<GeneratedFile> PruneFiles(IReadOnlyList<GeneratedFile> files, GenerationPlan? plan = null)
    {
        IEnumerable<GeneratedFile> query = files.Where(f => !IsDotNetOnlyArtifactPath(f.RelativePath));
        if (plan is not null && StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack)
            query = query.Where(f => !IsStrayNodeArtifactForJavaReact(f.RelativePath));
        return query.ToList();
    }

    internal static bool ShouldDropDotNetArtifacts(GenerationPlan plan)
    {
        if (plan.TechStack.Languages.Any(l =>
                l.Contains("c#", StringComparison.OrdinalIgnoreCase) ||
                l.Contains("csharp", StringComparison.OrdinalIgnoreCase)))
            return false;

        return plan.TechStack.Languages.Any(l =>
                   l.Contains("python", StringComparison.OrdinalIgnoreCase) ||
                   l.Equals("py", StringComparison.OrdinalIgnoreCase)) ||
               plan.TechStack.Frameworks.Any(f =>
                   f.Contains("flask", StringComparison.OrdinalIgnoreCase) ||
                   f.Contains("django", StringComparison.OrdinalIgnoreCase) ||
                   f.Contains("fastapi", StringComparison.OrdinalIgnoreCase)) ||
               plan.TechStack.Languages.Any(l =>
                   l.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
                   l.Contains("typescript", StringComparison.OrdinalIgnoreCase) ||
                   l.Equals("node", StringComparison.OrdinalIgnoreCase)) ||
               plan.TechStack.Frameworks.Any(f =>
                   f.Contains("express", StringComparison.OrdinalIgnoreCase) ||
                   f.Contains("next", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsStrayNodeArtifactForJavaReact(string relativePath)
    {
        var p = StackArtifactCompleteness.SanitizeRelativePath(relativePath);
        if (p.Length == 0)
            return true;

        if (p.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (p.StartsWith("upstream/", StringComparison.OrdinalIgnoreCase)
            || p.Equals("BOOTSTRAP_EVIDENCE.md", StringComparison.OrdinalIgnoreCase)
            || p.Equals("ADAPTATION_BRIDGE.md", StringComparison.OrdinalIgnoreCase)
            || p.Contains("kanban", StringComparison.OrdinalIgnoreCase))
            return true;

        var name = Path.GetFileName(p);
        if (name.Equals("package.json", StringComparison.OrdinalIgnoreCase)
            || name.Equals("index.js", StringComparison.OrdinalIgnoreCase)
            || name.Equals("server.js", StringComparison.OrdinalIgnoreCase))
            return true;

        if (p.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
            && !p.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
            && !p.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool IsDotNetOnlyArtifactPath(string relativePath)
    {
        var p = relativePath.Replace('\\', '/');
        var ext = Path.GetExtension(p).ToLowerInvariant();
        if (ext is ".cs" or ".csproj" or ".sln" or ".fs" or ".vbproj" or ".fsproj") return true;
        var name = Path.GetFileName(p);
        if (name.Equals("global.json", StringComparison.OrdinalIgnoreCase)) return true;
        return name.Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase);
    }
}
