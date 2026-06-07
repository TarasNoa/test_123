using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;

/// <summary>PHP (Laravel/Symfony) artifact normalization — Tier 1 golden path scaffold.</summary>
public static class PhpStackArtifactRecovery
{
    public static bool AppliesTo(GenerationPlan plan)
    {
        var kind = StackPlanHeuristics.Classify(plan);
        return kind is StackKind.Php or StackKind.PhpVueFullStack;
    }

    public static int Normalize(IList<GeneratedFile> files, GenerationPlan plan, List<string> warnings, bool autoFix)
    {
        if (!AppliesTo(plan))
            return 0;

        var fixes = RemoveDuplicateAuthControllers(files, warnings, autoFix);
        if (autoFix)
            fixes += Tier1CompileRemediationRouter.ApplyNormalize(files, plan, "php-laravel-vue");
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

    private static int RemoveDuplicateAuthControllers(IList<GeneratedFile> files, List<string> warnings, bool autoFix)
    {
        var controllers = files
            .Where(f => f.RelativePath.EndsWith(".php", StringComparison.OrdinalIgnoreCase)
                        && f.RelativePath.Contains("AuthController", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (controllers.Count <= 1)
            return 0;

        warnings.Add($"Multiple PHP AuthController files: {string.Join(", ", controllers.Select(c => c.RelativePath))}");
        if (!autoFix)
            return 0;

        var keep = controllers.OrderBy(c => c.RelativePath.Count(c => c == '/')).First();
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (controllers.Any(c => c.RelativePath.Equals(files[i].RelativePath, StringComparison.OrdinalIgnoreCase))
                && !files[i].RelativePath.Equals(keep.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }
}
