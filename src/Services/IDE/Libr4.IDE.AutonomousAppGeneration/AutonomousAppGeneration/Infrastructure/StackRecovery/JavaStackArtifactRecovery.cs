using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;

/// <summary>Java / Java+React recovery and normalization.</summary>
public static class JavaStackArtifactRecovery
{
    public static bool AppliesTo(GenerationPlan plan)
    {
        var kind = StackPlanHeuristics.Classify(plan);
        return kind is StackKind.Java or StackKind.JavaReactFullStack;
    }

    public static int Normalize(IList<GeneratedFile> files, GenerationPlan plan, List<string> warnings, bool autoFix)
    {
        if (!AppliesTo(plan))
            return 0;

        var fixes = 0;
        fixes += ReportDuplicatePackageRoots(files, warnings);
        fixes += RemoveDuplicateType(files, warnings, "AuthController", autoFix);
        fixes += RemoveDuplicateType(files, warnings, "UserRepository", autoFix);
        fixes += NormalizeJwt(files, plan, warnings, autoFix);

        if (autoFix)
        {
            fixes += JavaSpringCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>());
            fixes += JavaStructuralCompileRemediation.ApplyStructuralFixes(files, plan, null);
            var beforeCount = files.Count;
            var consolidated = JavaPackageRootConsolidator.Consolidate(files.ToList(), plan).ToList();
            if (consolidated.Count != beforeCount)
            {
                files.Clear();
                foreach (var file in consolidated)
                    files.Add(file);
                fixes++;
            }
        }

        return fixes;
    }

    public static int ApplyStructuralFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog) =>
        AppliesTo(plan)
            ? JavaStructuralCompileRemediation.ApplyStructuralFixes(files, plan, buildLog)
            : 0;

    public static int ApplyRuntimeFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog) =>
        RuntimeRecoveryService.ApplyJavaRuntimeFixes(files, plan, buildLog);

    public static int ApplyCompileFixes(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog) =>
        AppliesTo(plan) ? JavaSpringCompileRemediation.Apply(files, plan, errors) : 0;

    public static int ApplySecurityFixes(IList<GeneratedFile> files, GenerationPlan plan) =>
        AppliesTo(plan) ? JavaSpringSecurityRemediation.Apply(files, plan) : 0;

    private static int ReportDuplicatePackageRoots(IList<GeneratedFile> files, List<string> warnings)
    {
        var roots = files
            .Where(f => f.RelativePath.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                        && f.RelativePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
            .Select(JavaPackageRootConsolidator.ExtractPackageRoot)
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (roots.Count <= 1)
            return 0;
        warnings.Add($"Multiple application roots detected: {string.Join(", ", roots)}");
        return 0;
    }

    private static int NormalizeJwt(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        List<string> warnings,
        bool autoFix)
    {
        var jwtFiles = files
            .Where(f => f.RelativePath.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                        && f.RelativePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase)
                        && (f.RelativePath.Contains("Jwt", StringComparison.OrdinalIgnoreCase)
                            || f.RelativePath.Contains("Token", StringComparison.OrdinalIgnoreCase)
                            || f.Content?.Contains("Bearer", StringComparison.OrdinalIgnoreCase) == true))
            .Select(f => f.RelativePath)
            .ToList();
        if (jwtFiles.Count <= 3)
            return 0;

        warnings.Add($"JWT stack consolidation required ({jwtFiles.Count} files): {string.Join(", ", jwtFiles.Take(8))}");
        if (!autoFix)
            return 0;

        var removed = JwtStackNormalizer.Normalize(files, plan);
        if (removed > 0)
            warnings.Add($"JWT stack normalized: removed {removed} duplicate auth file(s).");
        return removed;
    }

    private static int RemoveDuplicateType(
        IList<GeneratedFile> files,
        List<string> warnings,
        string typeName,
        bool autoFix)
    {
        var matches = files
            .Where(f => f.RelativePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase)
                        && (f.Content?.Contains($"class {typeName}", StringComparison.Ordinal) == true
                            || f.Content?.Contains($"interface {typeName}", StringComparison.Ordinal) == true))
            .ToList();
        if (matches.Count <= 1)
            return 0;

        warnings.Add($"Multiple {typeName} definitions: {string.Join(", ", matches.Select(m => m.RelativePath))}");
        if (!autoFix)
            return 0;

        var keep = matches
            .OrderBy(m => m.RelativePath.Count(c => c == '/'))
            .ThenBy(m => m.RelativePath, StringComparer.OrdinalIgnoreCase)
            .First();
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (matches.Any(m => m.RelativePath.Equals(files[i].RelativePath, StringComparison.OrdinalIgnoreCase))
                && !files[i].RelativePath.Equals(keep.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }
}
