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
            .Select(b => new GenerationPhaseBatchResult(b.PhaseName, PruneFiles(b.Files)))
            .ToList();
    }

    public static IReadOnlyList<GeneratedFile> PruneFiles(IReadOnlyList<GeneratedFile> files) =>
        files.Where(f => !IsDotNetOnlyArtifactPath(f.RelativePath)).ToList();

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
