using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Removes kanban/repo-bootstrap contracts erroneously injected into Java+React banking plans.
/// </summary>
public static class BankingPlanSanitizer
{
    public static bool ShouldApply(GenerationPlan plan, string? userRequest) =>
        StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack
        && (StackPlanHeuristics.RequestsJavaBackendWithReactTypeScriptFrontend(userRequest ?? string.Empty)
            || LooksLikeBankingIntent($"{plan.ApplicationName} {plan.ApplicationDescription} {userRequest}"));

    public static GenerationPlan Sanitize(GenerationPlan plan, string? userRequest)
    {
        if (!ShouldApply(plan, userRequest))
            return plan;

        var description = StripContractBlocks(plan.ApplicationDescription);
        var phases = plan.Phases
            .Where(p => !IsRepoBootstrapPhase(p.Name))
            .Select((p, idx) => new GenerationPhase(
                idx + 1,
                p.Name,
                StripContractBlocks(p.Description),
                p.Assignments))
            .ToList();

        if (phases.Count == 0)
            phases = DefaultBankingPhases();

        var stripped = new GenerationPlan(
            plan.ApplicationName,
            description,
            plan.TechStack,
            phases,
            plan.RequiredAgents,
            plan.RuntimeImage,
            plan.BuildCommands,
            plan.TestCommands,
            plan.MaxIterations);

        return StackPlanHeuristics.AlignJavaReactFullStackPlan(stripped, userRequest);
    }

    private static bool LooksLikeBankingIntent(string blob) =>
        blob.Contains("bank", StringComparison.OrdinalIgnoreCase)
        || blob.Contains("fintech", StringComparison.OrdinalIgnoreCase)
        || blob.Contains("перевод", StringComparison.OrdinalIgnoreCase)
        || blob.Contains("платеж", StringComparison.OrdinalIgnoreCase)
        || blob.Contains("банк", StringComparison.OrdinalIgnoreCase);

    private static bool IsRepoBootstrapPhase(string name) =>
        name.Contains("repo bootstrap", StringComparison.OrdinalIgnoreCase)
        || name.Contains("bootstrap", StringComparison.OrdinalIgnoreCase) && name.Contains("adapt", StringComparison.OrdinalIgnoreCase);

    private static string StripContractBlocks(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var markers = new[]
        {
            "[REPO_BOOTSTRAP_CONTEXT]",
            "[/REPO_BOOTSTRAP_CONTEXT]",
            "[PRODUCT_QUALITY_LOCK_CONTRACT]",
            "[/PRODUCT_QUALITY_LOCK_CONTRACT]",
            "[[REPO_BOOTSTRAP_REQUIRED]]",
            "[[/REPO_BOOTSTRAP_REQUIRED]]",
            "[[AUTH_REQUIRED]]",
            "[[KANBAN_REQUIRED]]"
        };

        var result = text;
        foreach (var marker in markers)
        {
            while (true)
            {
                var start = result.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (start < 0)
                    break;

                var end = result.Length;
                var close = marker switch
                {
                    "[REPO_BOOTSTRAP_CONTEXT]" => "[/REPO_BOOTSTRAP_CONTEXT]",
                    "[PRODUCT_QUALITY_LOCK_CONTRACT]" => "[/PRODUCT_QUALITY_LOCK_CONTRACT]",
                    "[[REPO_BOOTSTRAP_REQUIRED]]" => "[[/REPO_BOOTSTRAP_REQUIRED]]",
                    _ => null
                };

                if (close is not null)
                {
                    var closeIdx = result.IndexOf(close, start, StringComparison.OrdinalIgnoreCase);
                    if (closeIdx >= 0)
                        end = closeIdx + close.Length;
                }
                else
                {
                    var nextLine = result.IndexOf('\n', start);
                    end = nextLine >= 0 ? nextLine + 1 : result.Length;
                }

                result = result.Remove(start, end - start);
            }
        }

        return result.Trim();
    }

    private static List<GenerationPhase> DefaultBankingPhases() =>
        new()
        {
            new(1, "Scaffold", "backend/ + frontend/ monorepo scaffold", Array.Empty<AgentAssignment>()),
            new(2, "Implement core", "accounts, transfers, payments, auth APIs and UI", Array.Empty<AgentAssignment>()),
            new(3, "Tests", "integration tests for banking flows", Array.Empty<AgentAssignment>())
        };
}
