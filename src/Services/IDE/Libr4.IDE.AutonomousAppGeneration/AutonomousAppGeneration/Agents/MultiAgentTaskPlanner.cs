using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Splits a generation phase into parallel implementer tasks (and optional subagent roles) for faster runs.
/// </summary>
public static class MultiAgentTaskPlanner
{
    public static List<AgentTask> CreateSingleTaskForPhase(
        AgentPhase phase,
        GenerationPlan plan,
        bool includeSubagentRoles = true)
    {
        var all = CreateTasksForPhase(phase, plan, includeSubagentRoles);
        if (all.Count <= 1)
            return all;

        var baseContext = BuildBaseContext(plan);
        var mergedDescription = string.Join("; ", all.Select(t => t.Description));
        var roles = includeSubagentRoles
            ? all.SelectMany(t => t.Subtasks)
                .Select(s => s.Context.TechStack)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
            : new List<string>();

        var single = new AgentTask
        {
            Description = mergedDescription,
            Context = CloneContext(baseContext, phase.ToString())
        };

        return new List<AgentTask> { WithSubtasks(single, roles) };
    }

    public static List<AgentTask> CreateTasksForPhase(
        AgentPhase phase,
        GenerationPlan plan,
        bool includeSubagentRoles = true)
    {
        var baseContext = BuildBaseContext(plan);

        return phase switch
        {
            AgentPhase.Backend => PlanBackend(plan, baseContext, includeSubagentRoles),
            AgentPhase.Frontend => PlanFrontend(plan, baseContext, includeSubagentRoles),
            AgentPhase.Database => PlanDatabase(plan, baseContext, includeSubagentRoles),
            AgentPhase.DevOps => SingleTask(phase, "Dockerfile and docker-compose for the app", baseContext),
            AgentPhase.CICD => SingleTask(phase, "CI workflow: build and test backend and frontend", baseContext),
            AgentPhase.Observability => SingleTask(phase, "Logging/metrics baseline (structured logs, health metrics)", baseContext),
            AgentPhase.Documentation => SingleTask(phase, "README and API overview for developers", baseContext),
            _ => SingleTask(phase, $"Generate {phase} artifacts for {plan.ApplicationName}", baseContext)
        };
    }

    private static List<AgentTask> PlanBackend(GenerationPlan plan, AgentContext baseContext, bool roles)
    {
        var isJavaReact = StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack;
        var isDjango = plan.TechStack.Frameworks.Any(f => f.Contains("django", StringComparison.OrdinalIgnoreCase));
        var domainHint = SummarizePlanIntent(plan, 220);
        var tasks = new List<AgentTask>
        {
            WithSubtasks(new AgentTask
            {
                Description = isJavaReact
                    ? "Spring Boot REST: accounts, transfers, payments controllers under backend/"
                    : isDjango
                        ? $"Django + DRF backend under backend/: {domainHint}"
                        : $"Backend REST API under backend/: {domainHint}",
                Context = CloneContext(baseContext, "Backend API surface")
            }, roles ? ["api-designer"] : Array.Empty<string>()),
            WithSubtasks(new AgentTask
            {
                Description = isJavaReact
                    ? "Auth: POST /api/auth/token, security stubs, backend/ Java"
                    : isDjango
                        ? "Django models, serializers, OpenAI Vision meal analysis service, POST /api/meals/analyze"
                        : "Core services, persistence layer, and API wiring for the planned backend",
                Context = CloneContext(baseContext, "Backend services")
            }, roles ? ["auth-specialist"] : Array.Empty<string>())
        };

        if (roles)
        {
            tasks.Add(WithSubtasks(new AgentTask
            {
                Description = "Backend business/integration tests (not health-only)",
                Context = CloneContext(baseContext, "Backend tests")
            }, ["qa-automation"]));
        }

        return tasks;
    }

    private static List<AgentTask> PlanFrontend(GenerationPlan plan, AgentContext baseContext, bool roles)
    {
        var isJavaReact = StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack;
        var isSolid = plan.TechStack.Frameworks.Any(f => f.Contains("solidjs", StringComparison.OrdinalIgnoreCase)
                                                         || f.Equals("solid", StringComparison.OrdinalIgnoreCase));
        var uiStack = isSolid ? "SolidJS + TypeScript + Vite" : "React TypeScript";
        var domainHint = SummarizePlanIntent(plan, 180);
        var tasks = new List<AgentTask>
        {
            WithSubtasks(new AgentTask
            {
                Description = isJavaReact
                    ? "React TypeScript UI: App shell, accounts list, wire to API client under frontend/"
                    : $"{uiStack} UI under frontend/: photo upload, analysis results, meal history — {domainHint}",
                Context = CloneContext(baseContext, "Frontend UI")
            }, roles ? ["css-expert"] : Array.Empty<string>()),
            WithSubtasks(new AgentTask
            {
                Description = isJavaReact
                    ? "frontend/src/api/client.ts — fetch accounts, transfers, auth token helpers"
                    : "frontend/src/api/client.ts — multipart image upload to /api/meals/analyze and meal history fetch",
                Context = CloneContext(baseContext, "Frontend API client")
            }, roles ? ["api-designer"] : Array.Empty<string>())
        };

        if (roles)
        {
            tasks.Add(WithSubtasks(new AgentTask
            {
                Description = "Vitest tests for API client and one UI smoke test",
                Context = CloneContext(baseContext, "Frontend tests")
            }, ["qa-automation"]));
        }

        return tasks;
    }

    private static List<AgentTask> PlanDatabase(GenerationPlan plan, AgentContext baseContext, bool roles)
    {
        var desc = StackPlanHeuristics.IsJava(plan)
            ? "Schema/migration stub: accounts, transactions (Flyway or SQL under backend/)"
            : StackLayoutHeuristics.UsesDjango(plan)
                ? "Django migration for domain models under backend/meals/migrations/"
                : "Database schema and migration stub aligned to the planned domain";

        return new List<AgentTask>
        {
            WithSubtasks(new AgentTask
            {
                Description = desc,
                Context = CloneContext(baseContext, "Database")
            }, roles ? ["db-architect"] : Array.Empty<string>())
        };
    }

    private static List<AgentTask> SingleTask(AgentPhase phase, string description, AgentContext ctx) =>
        new() { new AgentTask { Description = description, Context = CloneContext(ctx, phase.ToString()) } };

    private static AgentTask WithSubtasks(AgentTask task, IReadOnlyList<string> roles)
    {
        foreach (var role in roles)
        {
            var subtask = new AgentTask
            {
                Description = $"[{role}] {task.Description}",
                Context = new AgentContext
                {
                    ApplicationName = task.Context.ApplicationName,
                    Description = task.Description,
                    TechStack = role
                }
            };
            // Leaf subagent must not see parent Subtasks or it re-delegates forever via GenericImplementerAgent.
            subtask.Context.Task = subtask;
            task.Subtasks.Add(subtask);
        }

        task.Context.Task = task;
        return task;
    }

    private static string SummarizePlanIntent(GenerationPlan plan, int maxChars)
    {
        var text = plan.ApplicationDescription ?? string.Empty;
        var marker = text.IndexOf("[[", StringComparison.Ordinal);
        if (marker > 0)
            text = text[..marker];
        text = text.Replace('\n', ' ').Trim();
        return text.Length <= maxChars ? text : text[..maxChars] + "...";
    }

    private static bool UsesBackendFrontendLayout(GenerationPlan plan) =>
        StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack
        || plan.ApplicationDescription.Contains("backend/", StringComparison.OrdinalIgnoreCase)
        || plan.ApplicationDescription.Contains("[[DJANGO_SOLIDJS_FULLSTACK]]", StringComparison.Ordinal)
        || plan.ApplicationDescription.Contains("[[FASTAPI_REACT_FULLSTACK]]", StringComparison.Ordinal)
        || plan.ApplicationDescription.Contains("[[ASPNET_REACT_FULLSTACK]]", StringComparison.Ordinal);

    private static AgentContext BuildBaseContext(GenerationPlan plan)
    {
        var monorepoHint = UsesBackendFrontendLayout(plan)
            ? " Output paths MUST use backend/ and frontend/ prefixes. Return JSON {\"files\":[...]} only."
            : string.Empty;

        return new AgentContext
        {
            ApplicationName = plan.ApplicationName,
            Description = plan.ApplicationDescription + monorepoHint,
            TechStack = string.Join(", ", plan.TechStack.Languages.Concat(plan.TechStack.Frameworks))
        };
    }

    private static AgentContext CloneContext(AgentContext source, string scopeHint) =>
        new()
        {
            ApplicationName = source.ApplicationName,
            Description = $"{scopeHint}: {source.Description}",
            TechStack = source.TechStack
        };
}
