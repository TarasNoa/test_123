using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;

/// <summary>Python (FastAPI/Flask/Django) artifact normalization.</summary>
public static class PythonStackArtifactRecovery
{
    public static bool AppliesTo(GenerationPlan plan) =>
        StackPlanHeuristics.Classify(plan) == StackKind.Python;

    public static int Normalize(IList<GeneratedFile> files, GenerationPlan plan, List<string> warnings, bool autoFix)
    {
        if (!AppliesTo(plan))
            return 0;

        var fixes = 0;
        fixes += RemoveDuplicateAppFactories(files, warnings, autoFix);
        fixes += UniversalManifestFixes.FixRequirementsDuplicates(files, warnings, autoFix);
        if (autoFix)
        {
            fixes += EnsureRootRequirementsMirror(files);
            fixes += Tier1CompileRemediationRouter.ApplyNormalize(files, plan);
        }
        return fixes;
    }

    public static int ApplyStructuralFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog) => 0;

    public static int ApplyRuntimeFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog) =>
        RuntimeRecoveryService.ApplyPythonRuntimeFixes(files, buildLog);

    public static int ApplyCompileFixes(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog)
    {
        if (!AppliesTo(plan))
            return 0;

        var warnings = new List<string>();
        var fixes = UniversalManifestFixes.FixRequirementsDuplicates(files, warnings, autoFix: true);
        fixes += EnsureRootRequirementsMirror(files);
        fixes += PythonPytestImportRemediation.Apply(files, buildLog);
        fixes += Tier1CompileRemediationRouter.ApplyCompile(files, plan, errors, buildLog);
        return fixes;
    }

    public static int ApplySecurityFixes(IList<GeneratedFile> files, GenerationPlan plan) => 0;

    private static int RemoveDuplicateAppFactories(IList<GeneratedFile> files, List<string> warnings, bool autoFix)
    {
        var apps = files
            .Where(f => f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase)
                        && (f.Content?.Contains("FastAPI(", StringComparison.Ordinal) == true
                            || f.Content?.Contains("Flask(__name__)", StringComparison.Ordinal) == true
                            || f.Content?.Contains("Django", StringComparison.Ordinal) == true))
            .ToList();
        if (apps.Count <= 1)
            return 0;

        warnings.Add($"Multiple Python app entry modules: {string.Join(", ", apps.Select(a => a.RelativePath))}");
        if (!autoFix)
            return 0;

        var keep = apps
            .OrderBy(a => a.RelativePath.Equals("main.py", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(a => a.RelativePath.Equals("app.py", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(a => a.RelativePath.Count(c => c == '/'))
            .First();
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (apps.Any(a => a.RelativePath.Equals(files[i].RelativePath, StringComparison.OrdinalIgnoreCase))
                && !files[i].RelativePath.Equals(keep.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }

    /// <summary>Mirror nested requirements.txt to repo root when build commands expect ./requirements.txt.</summary>
    private static int EnsureRootRequirementsMirror(IList<GeneratedFile> files)
    {
        if (files.Any(f => f.RelativePath.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase)))
            return 0;

        var nested = files
            .FirstOrDefault(f => f.RelativePath.Equals("src/requirements.txt", StringComparison.OrdinalIgnoreCase)
                                 || f.RelativePath.Equals("backend/requirements.txt", StringComparison.OrdinalIgnoreCase));
        if (nested?.Content is null)
            return 0;

        files.Add(new GeneratedFile("requirements.txt", "text", nested.Content));
        return 1;
    }
}
