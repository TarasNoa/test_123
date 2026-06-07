using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Routes plan normalization to the active stack (Java+React, .NET, Python, Node, …).
/// </summary>
public static class StackPlanSanitizer
{
    public static bool ShouldApply(GenerationPlan plan, string? userRequest) =>
        JavaReactPlanSanitizer.ShouldApply(plan, userRequest)
        || GoldenStackPlanAligner.ShouldApply(plan, userRequest)
        || StackPlanHeuristics.ShouldPreferAspNetCoreForRepoBootstrap(userRequest);

    public static GenerationPlan Sanitize(GenerationPlan plan, string? userRequest)
    {
        plan = StrictStackContractEnforcer.Enforce(plan, userRequest);

        if (JavaReactPlanSanitizer.ShouldApply(plan, userRequest)
            && !StrictStackContractEnforcer.HasActiveContract(plan, userRequest))
            plan = JavaReactPlanSanitizer.Sanitize(plan, userRequest);

        if (GoldenStackPlanAligner.ShouldApply(plan, userRequest)
            && StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
            plan = GoldenStackPlanAligner.Align(plan, userRequest);

        if (StackPlanHeuristics.ShouldPreferAspNetCoreForRepoBootstrap(userRequest)
            && StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
            plan = StackPlanHeuristics.AlignAspNetCoreRepoBootstrapPlan(plan, userRequest);

        return StrictStackContractEnforcer.Enforce(plan, userRequest);
    }
}
