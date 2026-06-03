using System.Text.RegularExpressions;
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

    public static bool IsJava(GenerationPlan plan)
    {
        if (plan is null) return false;
        var langHit = plan.TechStack.Languages.Any(l =>
            Regex.IsMatch(l, @"\bjava\b", RegexOptions.IgnoreCase));
        var fwHit = plan.TechStack.Frameworks.Any(f =>
            f.Contains("spring", StringComparison.OrdinalIgnoreCase));
        var runtimeHit = !string.IsNullOrEmpty(plan.RuntimeImage)
            && (plan.RuntimeImage.Contains("temurin", StringComparison.OrdinalIgnoreCase)
                || plan.RuntimeImage.Contains("openjdk", StringComparison.OrdinalIgnoreCase)
                || plan.RuntimeImage.Contains("java", StringComparison.OrdinalIgnoreCase));
        return langHit || fwHit || runtimeHit;
    }

    public static bool IsReactTypeScriptFrontend(GenerationPlan plan)
    {
        if (plan is null) return false;
        var react = plan.TechStack.Frameworks.Any(f =>
            f.Contains("react", StringComparison.OrdinalIgnoreCase));
        var ts = plan.TechStack.Languages.Any(l =>
            l.Contains("typescript", StringComparison.OrdinalIgnoreCase))
            || plan.TechStack.Frameworks.Any(f =>
                f.Contains("typescript", StringComparison.OrdinalIgnoreCase));
        return react && ts;
    }

    public static StackKind Classify(GenerationPlan plan)
    {
        if (plan is null) return StackKind.Unknown;
        if (IsPython(plan)) return StackKind.Python;
        if (IsJava(plan) && IsReactTypeScriptFrontend(plan)) return StackKind.JavaReactFullStack;
        if (IsJava(plan)) return StackKind.Java;
        if (IsNode(plan)) return StackKind.Node;
        if (IsAspNetCore(plan)) return StackKind.DotNet;
        return StackKind.Unknown;
    }

    /// <summary>
    /// User explicitly requested Java/Spring backend and React+TypeScript frontend (e.g. mobile banking full-stack).
    /// </summary>
    public static bool RequestsJavaBackendWithReactTypeScriptFrontend(string? userRequest)
    {
        if (string.IsNullOrWhiteSpace(userRequest))
            return false;

        var s = userRequest.ToLowerInvariant();
        var wantsJava = Regex.IsMatch(s, @"\bjava\b")
                        || s.Contains("spring boot", StringComparison.Ordinal)
                        || s.Contains("spring ", StringComparison.Ordinal);
        var wantsReact = s.Contains("react", StringComparison.Ordinal)
                         || s.Contains("реакт", StringComparison.Ordinal);
        var wantsTypeScript = s.Contains("typescript", StringComparison.Ordinal)
                              || s.Contains("type script", StringComparison.Ordinal)
                              || s.Contains("tsx", StringComparison.Ordinal);

        var mentionsBackend = s.Contains("backend", StringComparison.Ordinal)
                              || s.Contains("бэкенд", StringComparison.Ordinal)
                              || s.Contains("бекенд", StringComparison.Ordinal)
                              || s.Contains("server", StringComparison.Ordinal)
                              || s.Contains("api", StringComparison.Ordinal);
        var mentionsFrontend = s.Contains("frontend", StringComparison.Ordinal)
                               || s.Contains("фронтенд", StringComparison.Ordinal)
                               || s.Contains("фронт", StringComparison.Ordinal)
                               || s.Contains("client", StringComparison.Ordinal)
                               || s.Contains("ui", StringComparison.Ordinal);

        var explicitSplit = wantsJava && wantsReact && wantsTypeScript
                            && (mentionsBackend || mentionsFrontend);

        // Also accept compact phrasing: "java ... react typescript" without separate backend/frontend words.
        var compactSplit = wantsJava && wantsReact && wantsTypeScript
                           && (s.Contains("mobile", StringComparison.Ordinal)
                               || s.Contains("банк", StringComparison.Ordinal)
                               || s.Contains("banking", StringComparison.Ordinal)
                               || s.Contains("fintech", StringComparison.Ordinal));

        return explicitSplit || compactSplit;
    }

    public static TechStack CreateJavaReactFullStackTechStack(TechStack? preserve = null) =>
        new(
            languages: new List<string> { "Java", "TypeScript" },
            frameworks: new List<string> { "Spring Boot", "React" },
            databases: preserve is not null && preserve.Databases.Count > 0
                ? preserve.Databases.ToList()
                : new List<string> { "PostgreSQL" },
            infrastructure: preserve is not null && preserve.Infrastructure.Count > 0
                ? preserve.Infrastructure.ToList()
                : new List<string> { "Docker", "Docker Compose" },
            rationale: "Explicit contract: Spring Boot Java backend + React TypeScript frontend; DB/infra chosen by planner.");

    public static GenerationPlan AlignJavaReactFullStackPlan(GenerationPlan plan, string? userRequest)
    {
        if (!RequestsJavaBackendWithReactTypeScriptFrontend(userRequest)
            && !(IsJava(plan) && IsReactTypeScriptFrontend(plan)))
            return plan;

        var techStack = CreateJavaReactFullStackTechStack(plan.TechStack);
        // eclipse-temurin JDK image has Java but not Maven/npm — bootstrap tools once per workspace.
        const string bootstrapTools =
            "export DEBIAN_FRONTEND=noninteractive && apt-get update -qq && apt-get install -y -qq maven npm > /dev/null";
        var build = new List<string>
        {
            bootstrapTools,
            "cd backend && mvn -q -DskipTests package",
            "cd frontend && npm ci && npm run build"
        };
        var test = new List<string>
        {
            bootstrapTools,
            "cd backend && mvn -q test",
            "cd frontend && npm test -- --watch=false"
        };

        var runtime = plan.RuntimeImage;
        if (string.IsNullOrWhiteSpace(runtime)
            || runtime.Contains("dotnet", StringComparison.OrdinalIgnoreCase)
            || runtime.Contains("python", StringComparison.OrdinalIgnoreCase)
            || runtime.Contains("node", StringComparison.OrdinalIgnoreCase))
            runtime = "eclipse-temurin:21-jdk";

        var phases = plan.Phases.ToList();
        if (!phases.Any(p => p.Name.Contains("backend", StringComparison.OrdinalIgnoreCase)))
        {
            phases.Add(new GenerationPhase(
                phases.Count + 1,
                "Backend API (Java)",
                "Spring Boot REST API: accounts, transfers, payments, auth, audit.",
                new[]
                {
                    new AgentAssignment("CodeGenerationAgent", "Backend", "Implement Java/Spring Boot services and controllers."),
                    new AgentAssignment("SecurityTestingAgent", "Security", "Validate auth, input validation, and regulated-domain controls.")
                }));
        }

        if (!phases.Any(p => p.Name.Contains("frontend", StringComparison.OrdinalIgnoreCase)))
        {
            phases.Add(new GenerationPhase(
                phases.Count + 1,
                "Frontend (React TypeScript)",
                "React+TS client: mobile banking flows wired to backend API.",
                new[]
                {
                    new AgentAssignment("CodeGenerationAgent", "Frontend", "Implement React TypeScript UI and API client."),
                    new AgentAssignment("CodeReviewAgent", "Review", "Verify UI/API contract alignment.")
                }));
        }

        phases = phases
            .Select((p, idx) => new GenerationPhase(idx + 1, p.Name, p.Description, p.Assignments))
            .ToList();

        var requiredAgents = plan.RequiredAgents.ToHashSet(StringComparer.OrdinalIgnoreCase);
        requiredAgents.Add("CodeGenerationAgent");
        requiredAgents.Add("SecurityTestingAgent");
        requiredAgents.Add("CodeReviewAgent");

        var description = plan.ApplicationDescription;
        if (!description.Contains("[[JAVA_REACT_FULLSTACK]]", StringComparison.Ordinal))
        {
            description +=
                "\n\n[[JAVA_REACT_FULLSTACK]]\n" +
                "backend=Java/Spring Boot\n" +
                "frontend=React+TypeScript\n" +
                "layout=backend/ + frontend/\n" +
                "reject_single_stack_substitution=true\n";
        }

        return new GenerationPlan(
            plan.ApplicationName,
            description,
            techStack,
            phases,
            requiredAgents.ToList(),
            runtime,
            build,
            test,
            plan.MaxIterations);
    }

    /// <summary>
    /// Repo-bootstrap product contract (JWT auth + kanban) targets ASP.NET Core unless the user
    /// explicitly requested Python or Node.
    /// </summary>
    public static bool ShouldPreferAspNetCoreForRepoBootstrap(string? userRequest)
    {
        if (string.IsNullOrWhiteSpace(userRequest))
            return true;

        var s = userRequest.ToLowerInvariant();
        var explicitPython = s.Contains("python", StringComparison.Ordinal)
                             || s.Contains("django", StringComparison.Ordinal)
                             || s.Contains("fastapi", StringComparison.Ordinal)
                             || s.Contains("flask", StringComparison.Ordinal);
        var explicitNode = s.Contains("node.js", StringComparison.Ordinal)
                           || s.Contains("nodejs", StringComparison.Ordinal)
                           || s.Contains("express", StringComparison.Ordinal)
                           || s.Contains("nestjs", StringComparison.Ordinal)
                           || s.Contains("next.js", StringComparison.Ordinal)
                           || s.Contains("nextjs", StringComparison.Ordinal)
                           || (s.Contains("javascript", StringComparison.Ordinal) && s.Contains("api", StringComparison.Ordinal))
                           || (s.Contains("typescript", StringComparison.Ordinal) && s.Contains("api", StringComparison.Ordinal));

        if (explicitPython || explicitNode)
            return false;

        return true;
    }

    public static TechStack CreateAspNetCoreTechStack(TechStack? preserve = null) =>
        new(
            languages: new List<string> { "C#" },
            frameworks: new List<string> { "ASP.NET Core", "EF Core" },
            databases: preserve is not null && preserve.Databases.Count > 0
                ? preserve.Databases.ToList()
                : new List<string> { "PostgreSQL" },
            infrastructure: preserve is not null && preserve.Infrastructure.Count > 0
                ? preserve.Infrastructure.ToList()
                : new List<string> { "Docker" },
            rationale: "Repo-bootstrap product contract requires ASP.NET Core adaptation.");

    public static GenerationPlan AlignAspNetCoreRepoBootstrapPlan(GenerationPlan plan, string? userRequest)
    {
        if (!ShouldPreferAspNetCoreForRepoBootstrap(userRequest))
            return plan;

        var techStack = CreateAspNetCoreTechStack(plan.TechStack);
        var build = plan.BuildCommands.ToList();
        if (build.Count == 0 || build.Any(c => c.Contains("npm", StringComparison.OrdinalIgnoreCase) || c.Contains("pip", StringComparison.OrdinalIgnoreCase)))
            build = new List<string> { "dotnet restore", "dotnet build" };

        var test = plan.TestCommands.ToList();
        if (test.Count == 0 || test.Any(c => c.Contains("npm", StringComparison.OrdinalIgnoreCase) || c.Contains("pytest", StringComparison.OrdinalIgnoreCase)))
            test = new List<string> { "dotnet test" };

        var runtime = plan.RuntimeImage;
        if (string.IsNullOrWhiteSpace(runtime)
            || runtime.Contains("node", StringComparison.OrdinalIgnoreCase)
            || runtime.Contains("python", StringComparison.OrdinalIgnoreCase))
            runtime = "mcr.microsoft.com/dotnet/sdk:8.0";

        return new GenerationPlan(
            plan.ApplicationName,
            plan.ApplicationDescription,
            techStack,
            plan.Phases,
            plan.RequiredAgents,
            runtime,
            build,
            test,
            plan.MaxIterations);
    }
}

public enum StackKind
{
    Unknown = 0,
    DotNet = 1,
    Python = 2,
    Node = 3,
    Java = 4,
    JavaReactFullStack = 5,
}
