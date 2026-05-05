using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed class AutonomousCodeConsistencyValidator : IAutonomousCodeConsistencyValidator
{
    private readonly AutonomousQualityGateOptions _options;

    public AutonomousCodeConsistencyValidator(IOptions<AutonomousQualityGateOptions> options)
    {
        _options = options.Value;
    }

    public QualityGateResult Validate(IReadOnlyList<GeneratedFile> files, GenerationPlan plan)
    {
        var reasons = new List<string>();
        var score = 10;

        // Only check C# files if the plan is actually .NET
        if (IsDotNetPlan(plan))
        {
            var csFiles = files.Where(f => f.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToList();
            if (csFiles.Count == 0)
            {
                score -= 5;
                reasons.Add("no_csharp_files");
            }

            foreach (var f in csFiles)
            {
                var c = f.Content ?? string.Empty;
                if (string.IsNullOrWhiteSpace(c))
                {
                    score -= 1;
                    reasons.Add($"empty_file:{f.RelativePath}");
                    continue;
                }

                if (c.Contains("// TODO", StringComparison.OrdinalIgnoreCase) || c.Contains("[your code here]", StringComparison.OrdinalIgnoreCase))
                {
                    score -= 1;
                    reasons.Add($"placeholder_code:{f.RelativePath}");
                }

                // Typical malformed token observed in generated files.
                if (c.Contains(",n", StringComparison.Ordinal))
                {
                    score -= 1;
                    reasons.Add($"syntax_artifact_comma_n:{f.RelativePath}");
                }

                // Authorization attribute without namespace import often breaks compile.
                if (c.Contains("[Authorize]", StringComparison.Ordinal) &&
                    !c.Contains("using Microsoft.AspNetCore.Authorization;", StringComparison.Ordinal))
                {
                    score -= 1;
                    reasons.Add($"missing_authorize_using:{f.RelativePath}");
                }

                // Naive brace balance sanity (outside strings) to catch truncated code blocks.
                if (!HasBalancedBraces(c))
                {
                    score -= 2;
                    reasons.Add($"unbalanced_braces:{f.RelativePath}");
                }
            }
        }

        // Ensure tests exist when requested.
        if (plan.TestCommands.Count > 0 && !HasStackAppropriateTests(files, plan))
        {
            score -= 2;
            reasons.Add("tests_missing_while_required");
        }

        score = Math.Clamp(score, 0, 10);
        return new QualityGateResult(
            "consistency",
            score,
            score >= Math.Clamp(_options.ConsistencyMinScore, 1, 10),
            reasons);
    }

    /// <summary>
    /// P1-9: delegate to single source of truth. Consistency uses the *exclusive* variant —
    /// .NET signals AND no Python/Node language present (matches legacy behaviour).
    /// </summary>
    private static bool IsDotNetPlan(GenerationPlan plan) => StackPlanHeuristics.IsDotNetExclusive(plan);

    private static bool HasStackAppropriateTests(IReadOnlyList<GeneratedFile> files, GenerationPlan plan)
    {
        if (IsDotNetPlan(plan))
            return files.Any(f => GenerationPathHeuristics.LooksLikeDotNetTestPath(f.RelativePath));

        if (IsPythonPlan(plan))
            return files.Any(f => GenerationPathHeuristics.LooksLikePythonTestPath(f.RelativePath));

        if (IsNodePlan(plan))
            return files.Any(f => GenerationPathHeuristics.LooksLikeNodeTestPath(f.RelativePath));

        return files.Any(f =>
            GenerationPathHeuristics.NormalizeSlashes(f.RelativePath).Contains("test", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPythonPlan(GenerationPlan plan) => StackPlanHeuristics.IsPython(plan);
    private static bool IsNodePlan(GenerationPlan plan) => StackPlanHeuristics.IsNode(plan);

    private static bool HasBalancedBraces(string text)
    {
        var braces = 0;
        var inString = false;
        var escape = false;

        foreach (var ch in text)
        {
            if (escape)
            {
                escape = false;
                continue;
            }

            if (inString)
            {
                if (ch == '\\') { escape = true; continue; }
                if (ch == '"') inString = false;
                continue;
            }

            if (ch == '"') { inString = true; continue; }
            if (ch == '{') braces++;
            if (ch == '}') braces--;
            if (braces < 0) return false;
        }

        return braces == 0 && !inString;
    }
}
