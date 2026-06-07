using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;

/// <summary>Ruby on Rails artifact normalization — Tier 2 standard remediation.</summary>
public static class RubyStackArtifactRecovery
{
    public static bool AppliesTo(GenerationPlan plan) =>
        StackPlanHeuristics.Classify(plan) == StackKind.Ruby;

    public static int Normalize(IList<GeneratedFile> files, GenerationPlan plan, List<string> warnings, bool autoFix)
    {
        if (!AppliesTo(plan))
            return 0;

        return RemoveDuplicateApplicationControllers(files, warnings, autoFix);
    }

    public static int ApplyStructuralFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog) => 0;

    public static int ApplyRuntimeFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog) => 0;

    public static int ApplyCompileFixes(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog) => 0;

    public static int ApplySecurityFixes(IList<GeneratedFile> files, GenerationPlan plan) => 0;

    private static int RemoveDuplicateApplicationControllers(IList<GeneratedFile> files, List<string> warnings, bool autoFix)
    {
        var controllers = files
            .Where(f => f.RelativePath.EndsWith(".rb", StringComparison.OrdinalIgnoreCase)
                        && f.RelativePath.Contains("application_controller", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (controllers.Count <= 1)
            return 0;

        warnings.Add($"Multiple Rails ApplicationController files: {string.Join(", ", controllers.Select(c => c.RelativePath))}");
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
