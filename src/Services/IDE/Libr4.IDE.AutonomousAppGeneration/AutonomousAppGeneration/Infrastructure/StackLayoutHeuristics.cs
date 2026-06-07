using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Detects monorepo layout and frontend presence for any planned stack.
/// </summary>
public static class StackLayoutHeuristics
{
    public static bool UsesBackendFrontendLayout(GenerationPlan plan)
    {
        if (plan is null) return false;

        if (plan.ApplicationDescription.Contains("backend/", StringComparison.OrdinalIgnoreCase)
            && plan.ApplicationDescription.Contains("frontend/", StringComparison.OrdinalIgnoreCase))
            return true;

        if (plan.BuildCommands.Any(c => c.Contains("cd backend", StringComparison.OrdinalIgnoreCase))
            && plan.BuildCommands.Any(c => c.Contains("cd frontend", StringComparison.OrdinalIgnoreCase)))
            return true;

        return StackPlanHeuristics.Classify(plan) is StackKind.JavaReactFullStack
            or StackKind.GoReactFullStack
            or StackKind.PhpVueFullStack
            || (HasBackendStack(plan) && HasSeparatedFrontend(plan));
    }

    public static bool HasSeparatedFrontend(GenerationPlan plan) =>
        plan.TechStack.Frameworks.Any(IsFrontendFramework)
        || plan.TechStack.Languages.Any(l =>
            l.Contains("typescript", StringComparison.OrdinalIgnoreCase)
            && HasBackendStack(plan));

    public static bool HasBackendStack(GenerationPlan plan) =>
        StackPlanHeuristics.IsPython(plan)
        || StackPlanHeuristics.IsJava(plan)
        || StackPlanHeuristics.IsGo(plan)
        || StackPlanHeuristics.IsRust(plan)
        || StackPlanHeuristics.IsPhp(plan)
        || StackPlanHeuristics.IsRuby(plan)
        || StackPlanHeuristics.IsNode(plan)
        || StackPlanHeuristics.IsAspNetCore(plan);

    public static bool UsesDjango(GenerationPlan plan) =>
        plan.TechStack.Frameworks.Any(f => f.Contains("django", StringComparison.OrdinalIgnoreCase))
        || plan.ApplicationDescription.Contains("django", StringComparison.OrdinalIgnoreCase);

    public static bool UsesFastApi(GenerationPlan plan) =>
        plan.TechStack.Frameworks.Any(f => f.Contains("fastapi", StringComparison.OrdinalIgnoreCase));

    public static bool UsesSolidJs(GenerationPlan plan) =>
        plan.TechStack.Frameworks.Any(f => f.Contains("solidjs", StringComparison.OrdinalIgnoreCase));

    public static bool UsesReact(GenerationPlan plan) =>
        plan.TechStack.Frameworks.Any(f => f.Contains("react", StringComparison.OrdinalIgnoreCase));

    public static bool UsesVue(GenerationPlan plan) =>
        plan.TechStack.Frameworks.Any(f => f.Contains("vue", StringComparison.OrdinalIgnoreCase)
                                           || f.Contains("nuxt", StringComparison.OrdinalIgnoreCase));

    public static string BackendRoot(GenerationPlan plan) =>
        UsesBackendFrontendLayout(plan) ? "backend/" : "src/";

    public static string FrontendRoot(GenerationPlan plan) => "frontend/";

    public static string ProjectSlug(GenerationPlan plan)
    {
        var raw = new string((plan.ApplicationName ?? "app")
            .Where(char.IsLetterOrDigit)
            .ToArray());
        return string.IsNullOrWhiteSpace(raw) ? "app" : raw.ToLowerInvariant();
    }

    private static bool IsFrontendFramework(string framework) =>
        framework.Contains("react", StringComparison.OrdinalIgnoreCase)
        || framework.Contains("solidjs", StringComparison.OrdinalIgnoreCase)
        || framework.Contains("solid", StringComparison.OrdinalIgnoreCase)
        || framework.Contains("vue", StringComparison.OrdinalIgnoreCase)
        || framework.Contains("nuxt", StringComparison.OrdinalIgnoreCase)
        || framework.Contains("svelte", StringComparison.OrdinalIgnoreCase)
        || framework.Contains("angular", StringComparison.OrdinalIgnoreCase)
        || framework.Contains("blazor", StringComparison.OrdinalIgnoreCase);
}
