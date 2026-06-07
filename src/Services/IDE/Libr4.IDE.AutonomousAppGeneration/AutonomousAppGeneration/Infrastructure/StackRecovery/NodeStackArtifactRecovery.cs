using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;

/// <summary>Node.js / TypeScript / React frontend recovery.</summary>
public static class NodeStackArtifactRecovery
{
    public static bool AppliesTo(GenerationPlan plan)
    {
        var kind = StackPlanHeuristics.Classify(plan);
        return kind == StackKind.Node || kind == StackKind.JavaReactFullStack;
    }

    public static int Normalize(IList<GeneratedFile> files, GenerationPlan plan, List<string> warnings, bool autoFix)
    {
        if (!AppliesTo(plan))
            return 0;

        var fixes = 0;
        fixes += RemoveDuplicateExpressServers(files, warnings, autoFix);
        fixes += UniversalManifestFixes.FixPackageJsonTemplateBraces(files);
        if (autoFix)
            fixes += Tier1CompileRemediationRouter.ApplyNormalize(files, plan);
        return fixes;
    }

    public static int ApplyStructuralFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog) =>
        UniversalManifestFixes.FixPackageJsonTemplateBraces(files);

    public static int ApplyRuntimeFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog) =>
        RuntimeRecoveryService.ApplyNodeRuntimeFixes(files, buildLog);

    public static int ApplyCompileFixes(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog) =>
        !AppliesTo(plan)
            ? 0
            : ReactFrontendRemediation.Apply(files, plan, errors)
              + Tier1CompileRemediationRouter.ApplyCompile(files, plan, errors, buildLog);

    public static int ApplySecurityFixes(IList<GeneratedFile> files, GenerationPlan plan) => 0;

    private static int RemoveDuplicateExpressServers(IList<GeneratedFile> files, List<string> warnings, bool autoFix)
    {
        var servers = files
            .Where(f => (f.RelativePath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
                         || f.RelativePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                        && (f.Content?.Contains("express()", StringComparison.OrdinalIgnoreCase) == true
                            || f.Content?.Contains("createServer(", StringComparison.OrdinalIgnoreCase) == true
                            || f.RelativePath.Contains("server", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (servers.Count <= 1)
            return 0;

        warnings.Add($"Multiple Node server entry files: {string.Join(", ", servers.Select(s => s.RelativePath))}");
        if (!autoFix)
            return 0;

        var keep = servers
            .OrderBy(s => s.RelativePath.Contains("frontend/", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(s => s.RelativePath.Count(c => c == '/'))
            .First();
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (servers.Any(s => s.RelativePath.Equals(files[i].RelativePath, StringComparison.OrdinalIgnoreCase))
                && !files[i].RelativePath.Equals(keep.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }
}
