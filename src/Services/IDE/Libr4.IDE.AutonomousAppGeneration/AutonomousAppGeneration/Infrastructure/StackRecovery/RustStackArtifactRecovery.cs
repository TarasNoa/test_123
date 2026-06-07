using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;

/// <summary>Rust (Axum/Actix) artifact normalization — Tier 1 golden path scaffold.</summary>
public static class RustStackArtifactRecovery
{
    public static bool AppliesTo(GenerationPlan plan) =>
        StackPlanHeuristics.Classify(plan) == StackKind.Rust;

    public static int Normalize(IList<GeneratedFile> files, GenerationPlan plan, List<string> warnings, bool autoFix)
    {
        if (!AppliesTo(plan))
            return 0;

        var fixes = RemoveDuplicateMainRs(files, warnings, autoFix);
        if (autoFix)
            fixes += Tier1CompileRemediationRouter.ApplyNormalize(files, plan, "rust-axum");
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

    private static int RemoveDuplicateMainRs(IList<GeneratedFile> files, List<string> warnings, bool autoFix)
    {
        var mains = files
            .Where(f => f.RelativePath.EndsWith("main.rs", StringComparison.OrdinalIgnoreCase)
                        || (f.RelativePath.EndsWith(".rs", StringComparison.OrdinalIgnoreCase)
                            && f.Content?.Contains("fn main()", StringComparison.Ordinal) == true))
            .ToList();
        if (mains.Count <= 1)
            return 0;

        warnings.Add($"Multiple Rust main entry files: {string.Join(", ", mains.Select(m => m.RelativePath))}");
        if (!autoFix)
            return 0;

        var keep = mains.OrderBy(m => m.RelativePath.Count(c => c == '/')).First();
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (mains.Any(m => m.RelativePath.Equals(files[i].RelativePath, StringComparison.OrdinalIgnoreCase))
                && !files[i].RelativePath.Equals(keep.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }
}
