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
        var isJava = StackPlanHeuristics.IsJava(plan);
        var tasks = new List<AgentTask>
        {
            WithSubtasks(new AgentTask
            {
                Description = isJava
                    ? "Spring Boot REST: accounts, transfers, payments controllers under backend/"
                    : "Backend REST API: accounts, transfers, payments endpoints",
                Context = CloneContext(baseContext, "Backend API surface")
            }, roles ? ["api-designer"] : Array.Empty<string>()),
            WithSubtasks(new AgentTask
            {
                Description = isJava
                    ? "Auth: POST /api/auth/token, security stubs, backend/ Java"
                    : "Authentication: token issuance and protected route stubs",
                Context = CloneContext(baseContext, "Backend auth")
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
        var tasks = new List<AgentTask>
        {
            WithSubtasks(new AgentTask
            {
                Description = "React TypeScript UI: App shell, accounts list, wire to API client under frontend/",
                Context = CloneContext(baseContext, "Frontend UI")
            }, roles ? ["css-expert"] : Array.Empty<string>()),
            WithSubtasks(new AgentTask
            {
                Description = "frontend/src/api/client.ts — fetch accounts, transfers, auth token helpers",
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
            : "Database schema and migration stub for accounts and transactions";

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
            task.Subtasks.Add(new AgentTask
            {
                Description = $"[{role}] {task.Description}",
                Context = new AgentContext
                {
                    ApplicationName = task.Context.ApplicationName,
                    Description = task.Description,
                    TechStack = role,
                    Task = task
                }
            });
        }

        task.Context.Task = task;
        return task;
    }

    private static AgentContext BuildBaseContext(GenerationPlan plan)
    {
        var monorepoHint = StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack
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
