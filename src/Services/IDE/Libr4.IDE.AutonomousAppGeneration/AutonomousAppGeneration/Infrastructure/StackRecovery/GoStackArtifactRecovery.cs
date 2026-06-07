using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;

/// <summary>Go (Gin/Echo/Fiber) artifact normalization — Tier 1 golden path scaffold.</summary>
public static class GoStackArtifactRecovery
{
    public static bool AppliesTo(GenerationPlan plan)
    {
        var kind = StackPlanHeuristics.Classify(plan);
        return kind is StackKind.Go or StackKind.GoReactFullStack;
    }

    public static int Normalize(IList<GeneratedFile> files, GenerationPlan plan, List<string> warnings, bool autoFix)
    {
        if (!AppliesTo(plan))
            return 0;

        var fixes = RemoveDuplicateMainPackages(files, warnings, autoFix);
        if (autoFix)
            fixes += Tier1CompileRemediationRouter.ApplyNormalize(files, plan, "go-gin-react");
        return fixes;
    }

    public static int ApplyStructuralFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog) => 0;

    public static int ApplyRuntimeFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog) => 0;

    public static int ApplyCompileFixes(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog) =>
        AppliesTo(plan) ? Tier1CompileRemediationRouter.ApplyCompile(files, plan, errors, buildLog) : 0;

    public static int ApplySecurityFixes(IList<GeneratedFile> files, GenerationPlan plan) => 0;

    private static int RemoveDuplicateMainPackages(IList<GeneratedFile> files, List<string> warnings, bool autoFix)
    {
        var mains = files
            .Where(f => f.RelativePath.EndsWith(".go", StringComparison.OrdinalIgnoreCase)
                        && (f.Content?.Contains("func main()", StringComparison.Ordinal) == true
                            || f.RelativePath.EndsWith("main.go", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (mains.Count <= 1)
            return 0;

        warnings.Add($"Multiple Go main packages: {string.Join(", ", mains.Select(m => m.RelativePath))}");
        if (!autoFix)
            return 0;

        var keep = mains
            .OrderBy(m => m.RelativePath.Contains("backend/", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(m => m.RelativePath.Count(c => c == '/'))
            .First();
        return RemoveAllBut(files, mains, keep.RelativePath);
    }

    private static int RemoveAllBut(IList<GeneratedFile> files, List<GeneratedFile> matches, string keepPath)
    {
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (matches.Any(m => m.RelativePath.Equals(files[i].RelativePath, StringComparison.OrdinalIgnoreCase))
                && !files[i].RelativePath.Equals(keepPath, StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }
}
