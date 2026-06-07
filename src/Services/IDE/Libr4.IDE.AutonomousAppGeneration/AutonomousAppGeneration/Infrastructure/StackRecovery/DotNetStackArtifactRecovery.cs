using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;

/// <summary>ASP.NET Core / C# artifact normalization.</summary>
public static class DotNetStackArtifactRecovery
{
    public static bool AppliesTo(GenerationPlan plan) =>
        StackPlanHeuristics.Classify(plan) == StackKind.DotNet
        || StackPlanHeuristics.IsAspNetCore(plan);

    public static int Normalize(IList<GeneratedFile> files, GenerationPlan plan, List<string> warnings, bool autoFix)
    {
        if (!AppliesTo(plan))
            return 0;

        var fixes = 0;
        fixes += RemoveDuplicateEntryPoints(files, warnings, autoFix);
        if (autoFix)
        {
            fixes += CsprojPackageReconciler.ReconcilePackages(files) > 0 ? 1 : 0;
            fixes += Tier1CompileRemediationRouter.ApplyNormalize(files, plan);
        }
        return fixes;
    }

    public static int ApplyStructuralFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog)
    {
        if (!AppliesTo(plan))
            return 0;
        return CsprojPackageReconciler.ReconcilePackages(files) > 0 ? 1 : 0;
    }

    public static int ApplyRuntimeFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog) =>
        RuntimeRecoveryService.ApplyDotNetRuntimeFixes(files, buildLog);

    public static int ApplyCompileFixes(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog) =>
        !AppliesTo(plan)
            ? 0
            : (CsprojPackageReconciler.ReconcilePackages(files) > 0 ? 1 : 0)
              + Tier1CompileRemediationRouter.ApplyCompile(files, plan, errors, buildLog);

    public static int ApplySecurityFixes(IList<GeneratedFile> files, GenerationPlan plan) => 0;

    private static int RemoveDuplicateEntryPoints(IList<GeneratedFile> files, List<string> warnings, bool autoFix)
    {
        var programs = files
            .Where(f => f.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                        && (f.Content?.Contains("WebApplication.CreateBuilder", StringComparison.Ordinal) == true
                            || f.RelativePath.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (programs.Count <= 1)
            return 0;

        warnings.Add($"Multiple .NET entry points: {string.Join(", ", programs.Select(p => p.RelativePath))}");
        if (!autoFix)
            return 0;

        var keep = programs
            .OrderBy(p => p.RelativePath.Count(c => c == '/'))
            .ThenBy(p => p.RelativePath, StringComparer.OrdinalIgnoreCase)
            .First();
        return RemoveAllBut(files, programs, keep.RelativePath);
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
