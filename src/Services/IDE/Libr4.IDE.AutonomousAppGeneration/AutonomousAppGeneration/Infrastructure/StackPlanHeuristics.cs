using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// P0-9 of audit roadmap: single source of truth for stack-plan classification.
/// Existing copies in <c>GenerationStackSafetyNet</c>, <c>LlmCodeGenerationService</c>,
/// <c>AutonomousQualityGateService</c> and <c>AutonomousCodeConsistencyValidator</c>
/// should delegate to these helpers (planned migration in P1-9).
/// </summary>
public static class StackPlanHeuristics
{
    /// <summary>
    /// True when the plan targets ASP.NET Core specifically (used by code-gen safety-net
    /// to decide whether to inject Controllers/Services/Models scaffolding).
    /// Stricter than <see cref="IsDotNet"/>: requires framework / runtime / api-intent.
    /// </summary>
    public static bool IsAspNetCore(GenerationPlan plan)
    {
        if (plan is null) return false;
        if (IsPython(plan) || IsNode(plan)) return false;

        var hasDotNetLanguage = HasDotNetLanguage(plan);
        var hasDotNetFramework = HasDotNetFramework(plan);
        var hasDotNetRuntime = HasDotNetRuntime(plan);

        var apiIntent = !string.IsNullOrEmpty(plan.ApplicationDescription)
            && plan.ApplicationDescription.Contains("api", StringComparison.OrdinalIgnoreCase);

        return hasDotNetFramework
            || hasDotNetRuntime
            || (hasDotNetLanguage && apiIntent);
    }

    /// <summary>
    /// Broad .NET classification (matches legacy <c>AutonomousQualityGateService.IsDotNetPlan</c>):
    /// any C# / .NET language, any asp.net / dotnet framework, or a dotnet runtime image.
    /// Does NOT require api-intent and does NOT exclude Python/Node.
    /// </summary>
    public static bool IsDotNet(GenerationPlan plan)
    {
        if (plan is null) return false;
        return HasDotNetLanguage(plan) || HasDotNetFramework(plan) || HasDotNetRuntime(plan);
    }

    /// <summary>
    /// Exclusive .NET classification (matches legacy <c>AutonomousCodeConsistencyValidator.IsDotNetPlan</c>):
    /// .NET signals AND no Python / Node language present.
    /// </summary>
    public static bool IsDotNetExclusive(GenerationPlan plan)
    {
        if (plan is null) return false;
        if (IsPython(plan) || IsNode(plan)) return false;
        return IsDotNet(plan);
    }

    private static bool HasDotNetLanguage(GenerationPlan plan) =>
        plan.TechStack.Languages.Any(l =>
            l.Contains("c#", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("csharp", StringComparison.OrdinalIgnoreCase) ||
            l.Contains(".net", StringComparison.OrdinalIgnoreCase));

    private static bool HasDotNetFramework(GenerationPlan plan) =>
        plan.TechStack.Frameworks.Any(f =>
            f.Contains("asp.net", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("aspnet", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("dotnet", StringComparison.OrdinalIgnoreCase));

    private static bool HasDotNetRuntime(GenerationPlan plan) =>
        !string.IsNullOrEmpty(plan.RuntimeImage)
        && plan.RuntimeImage.Contains("dotnet", StringComparison.OrdinalIgnoreCase);

    public static bool IsPython(GenerationPlan plan)
    {
        if (plan is null) return false;
        var langHit = plan.TechStack.Languages.Any(l =>
            l.Contains("python", StringComparison.OrdinalIgnoreCase) ||
            l.Equals("py", StringComparison.OrdinalIgnoreCase));
        var fwHit = plan.TechStack.Frameworks.Any(f =>
            f.Contains("flask", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("django", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("fastapi", StringComparison.OrdinalIgnoreCase));
        var runtimeHit = !string.IsNullOrEmpty(plan.RuntimeImage)
            && plan.RuntimeImage.Contains("python", StringComparison.OrdinalIgnoreCase);
        return langHit || fwHit || runtimeHit;
    }

    public static bool IsNode(GenerationPlan plan)
    {
        if (plan is null) return false;
        var langHit = plan.TechStack.Languages.Any(l =>
            l.Equals("javascript", StringComparison.OrdinalIgnoreCase) ||
            l.Equals("typescript", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("node", StringComparison.OrdinalIgnoreCase));
        var fwHit = plan.TechStack.Frameworks.Any(f =>
            f.Contains("express", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("next", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("react", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("node", StringComparison.OrdinalIgnoreCase));
        var runtimeHit = !string.IsNullOrEmpty(plan.RuntimeImage)
            && plan.RuntimeImage.Contains("node", StringComparison.OrdinalIgnoreCase);
        return langHit || fwHit || runtimeHit;
    }

    public static StackKind Classify(GenerationPlan plan)
    {
        if (plan is null) return StackKind.Unknown;
        if (IsPython(plan)) return StackKind.Python;
        if (IsNode(plan)) return StackKind.Node;
        if (IsAspNetCore(plan)) return StackKind.DotNet;
        return StackKind.Unknown;
    }
}

public enum StackKind
{
    Unknown = 0,
    DotNet = 1,
    Python = 2,
    Node = 3
}
