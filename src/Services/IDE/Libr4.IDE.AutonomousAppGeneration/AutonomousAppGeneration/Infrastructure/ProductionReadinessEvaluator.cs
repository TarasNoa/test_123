using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Heuristic score for Java+React banking runs (MVP vs production-oriented).
/// </summary>
public static class ProductionReadinessEvaluator
{
    public const int ProductionTargetScore = 75;

    public sealed record Evaluation(int Score, IReadOnlyList<string> Issues, bool IsProductionGrade);

    public static Evaluation Evaluate(GenerationPlan plan, IReadOnlyList<GeneratedFile> files)
    {
        if (StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
            return new Evaluation(100, Array.Empty<string>(), true);

        var issues = new List<string>();
        var score = 100;
        var paths = files.Select(f => StackArtifactCompleteness.SanitizeRelativePath(f.RelativePath)).ToList();
        var combined = string.Join('\n', files.Select(f => f.Content ?? string.Empty));

        if (!paths.Any(p => p.Equals("backend/pom.xml", StringComparison.OrdinalIgnoreCase)))
        {
            score -= 15;
            issues.Add("missing_backend_pom");
        }

        if (!paths.Any(p => p.StartsWith("backend/", StringComparison.OrdinalIgnoreCase) && p.EndsWith(".java", StringComparison.OrdinalIgnoreCase)))
        {
            score -= 20;
            issues.Add("missing_backend_java");
        }

        if (!paths.Any(p => p.Contains("/service/", StringComparison.OrdinalIgnoreCase) && p.EndsWith(".java", StringComparison.OrdinalIgnoreCase)))
        {
            score -= 10;
            issues.Add("missing_service_layer");
        }

        if (!paths.Any(p => p.Contains("application.yml", StringComparison.OrdinalIgnoreCase) || p.Contains("application.properties", StringComparison.OrdinalIgnoreCase)))
        {
            score -= 8;
            issues.Add("missing_application_config");
        }

        if (!combined.Contains("/api/transfers", StringComparison.OrdinalIgnoreCase)
            || !combined.Contains("/api/payments", StringComparison.OrdinalIgnoreCase))
        {
            score -= 12;
            issues.Add("missing_transfer_or_payment_api");
        }

        if (!combined.Contains("/api/auth", StringComparison.OrdinalIgnoreCase))
        {
            score -= 8;
            issues.Add("missing_auth_api");
        }

        var testBlob = string.Join('\n', files.Where(f => f.RelativePath.Contains("test", StringComparison.OrdinalIgnoreCase)).Select(f => f.Content));
        if (testBlob.Contains("contextLoads", StringComparison.Ordinal) && !testBlob.Contains("MockMvc", StringComparison.Ordinal) && !testBlob.Contains("transfer", StringComparison.OrdinalIgnoreCase))
        {
            score -= 15;
            issues.Add("superficial_backend_tests");
        }

        if (!combined.Contains("createTransfer", StringComparison.OrdinalIgnoreCase))
        {
            score -= 8;
            issues.Add("frontend_missing_transfer_client");
        }

        if (!paths.Any(p => p.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase) && p.EndsWith(".test.ts", StringComparison.OrdinalIgnoreCase))
            && !paths.Any(p => p.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase) && p.EndsWith(".test.tsx", StringComparison.OrdinalIgnoreCase)))
        {
            score -= 5;
            issues.Add("missing_frontend_tests");
        }

        score = Math.Clamp(score, 0, 100);
        return new Evaluation(score, issues, score >= ProductionTargetScore);
    }
}
