using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Aligns generation plans to Tier 1/2 golden stack paths (Java+React model for all production stacks).
/// </summary>
public static class GoldenStackPlanAligner
{
    public static GoldenStackPath? Detect(GenerationPlan plan, string? userRequest) =>
        GoldenStackPathRegistry.DetectFromRequest(userRequest, plan)
        ?? DetectFromPlan(plan);

    public static GenerationPlan Align(GenerationPlan plan, string? userRequest)
    {
        var golden = Detect(plan, userRequest);
        if (golden is null)
            return plan;

        if (golden.Id == "java-spring-react"
            && !StrictStackContractEnforcer.HasActiveContract(plan, userRequest))
            return StackPlanHeuristics.AlignJavaReactFullStackPlan(plan, userRequest);

        if (StrictStackContractEnforcer.HasActiveContract(plan, userRequest))
            return AlignGoldenPathPreserveStack(plan, golden);

        return AlignGoldenPath(plan, golden);
    }

    public static bool ShouldApply(GenerationPlan plan, string? userRequest) =>
        Detect(plan, userRequest) is not null;

    private static GoldenStackPath? DetectFromPlan(GenerationPlan plan)
    {
        var blob = string.Join(' ',
            plan.ApplicationDescription,
            string.Join(' ', plan.TechStack.Languages),
            string.Join(' ', plan.TechStack.Frameworks)).ToLowerInvariant();

        return GoldenStackPathRegistry.AllPaths
            .FirstOrDefault(p => GoldenStackPathRegistry.MatchesPath(blob, p));
    }

    private static GenerationPlan AlignGoldenPathPreserveStack(GenerationPlan plan, GoldenStackPath golden)
    {
        var phases = EnsurePhases(plan, golden);
        var description = EnsureContractMarker(plan.ApplicationDescription, golden);

        return new GenerationPlan(
            plan.ApplicationName,
            description,
            plan.TechStack,
            phases,
            plan.RequiredAgents,
            string.IsNullOrWhiteSpace(plan.RuntimeImage) ? golden.RuntimeImage : plan.RuntimeImage,
            plan.BuildCommands.Count > 0 ? plan.BuildCommands.ToList() : golden.BuildCommands.ToList(),
            plan.TestCommands.Count > 0 ? plan.TestCommands.ToList() : golden.TestCommands.ToList(),
            plan.MaxIterations);
    }

    private static GenerationPlan AlignGoldenPath(GenerationPlan plan, GoldenStackPath golden)
    {
        var techStack = new TechStack(
            golden.Languages.ToList(),
            golden.BackendFrameworks.Concat(golden.FrontendFrameworks).ToList(),
            plan.TechStack.Databases.Count > 0 ? plan.TechStack.Databases.ToList() : new List<string> { "PostgreSQL" },
            plan.TechStack.Infrastructure.Count > 0 ? plan.TechStack.Infrastructure.ToList() : new List<string> { "Docker" },
            $"Golden path: {golden.DisplayName}; layout={golden.Layout}");

        var phases = EnsurePhases(plan, golden);
        var description = EnsureContractMarker(plan.ApplicationDescription, golden);

        return new GenerationPlan(
            plan.ApplicationName,
            description,
            techStack,
            phases,
            plan.RequiredAgents,
            golden.RuntimeImage,
            golden.BuildCommands.ToList(),
            golden.TestCommands.ToList(),
            plan.MaxIterations);
    }

    private static string EnsureContractMarker(string description, GoldenStackPath golden)
    {
        if (description.Contains(golden.ContractMarker, StringComparison.Ordinal))
            return description;

        return description +
               $"\n\n{golden.ContractMarker}\n" +
               $"stack={golden.DisplayName}\n" +
               $"layout={golden.Layout}\n" +
               $"tier={golden.Tier}\n" +
               $"remediation={golden.RemediationDepth}\n" +
               "reject_single_stack_substitution=true\n";
    }

    private static List<GenerationPhase> EnsurePhases(GenerationPlan plan, GoldenStackPath golden)
    {
        var phases = plan.Phases.ToList();
        var hasBackend = golden.BackendFrameworks.Count > 0
                         && phases.Any(p => p.Name.Contains("backend", StringComparison.OrdinalIgnoreCase));
        var hasFrontend = golden.FrontendFrameworks.Count > 0
                          && phases.Any(p => p.Name.Contains("frontend", StringComparison.OrdinalIgnoreCase));

        if (golden.BackendFrameworks.Count > 0 && !hasBackend)
        {
            phases.Add(new GenerationPhase(
                phases.Count + 1,
                $"Backend ({golden.BackendFrameworks[0]})",
                $"{golden.BackendFrameworks[0]} API and services.",
                new[] { new AgentAssignment("CodeGenerationAgent", "Backend", $"Implement {golden.DisplayName} backend.") }));
        }

        if (golden.FrontendFrameworks.Count > 0 && !hasFrontend)
        {
            phases.Add(new GenerationPhase(
                phases.Count + 1,
                $"Frontend ({golden.FrontendFrameworks[0]})",
                $"{golden.FrontendFrameworks[0]} client wired to backend API.",
                new[] { new AgentAssignment("CodeGenerationAgent", "Frontend", $"Implement {golden.DisplayName} frontend.") }));
        }

        return phases
            .Select((p, idx) => new GenerationPhase(idx + 1, p.Name, p.Description, p.Assignments))
            .ToList();
    }
}
