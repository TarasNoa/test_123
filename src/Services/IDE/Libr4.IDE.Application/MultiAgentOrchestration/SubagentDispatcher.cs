using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.MultiAgentOrchestration;

// ── Интерфейс ────────────────────────────────────────────────────────────────

/// <summary>
/// Диспетчер субагентов.
/// Сениор-агент вызывает его когда ему нужна специализированная помощь:
///   Senior Backend → DispatchAsync("DBDesigner", "Create schema for users and accounts")
///   Senior Frontend → DispatchAsync("UIComponents", "Build login form and dashboard")
/// </summary>
public interface ISubagentDispatcher
{
    /// <summary>
    /// Вызывает субагента нужного типа и возвращает его результат.
    /// Результат автоматически сохраняется в AgentContext.SubagentResults.
    /// </summary>
    Task<SubagentResult> DispatchAsync(
        string       subagentType,
        string       task,
        AgentContext context,
        CancellationToken ct = default);
}

// ── Реализация ───────────────────────────────────────────────────────────────

/// <summary>
/// Реализация диспетчера субагентов.
/// Каждый тип субагента — это специализированный системный промпт + вызов LLM.
/// Результат идёт обратно сениору который его вызвал.
/// </summary>
public sealed class SubagentDispatcher : ISubagentDispatcher
{
    private readonly IAIService _ai;
    private readonly ILogger<SubagentDispatcher> _logger;

    // Системные промпты для каждого типа субагента
    private static readonly Dictionary<string, string> SubagentPrompts = new()
    {
        ["DBDesigner"] = """
            You are a Senior Database Architect specializing in PostgreSQL.
            Your task: design a production-ready database schema.
            Output: SQL DDL with CREATE TABLE statements, indexes, foreign keys.
            Follow these rules:
            - Use UUIDs for primary keys
            - Add created_at/updated_at timestamps
            - Add soft delete (deleted_at) where appropriate
            - Normalize to 3NF minimum
            - Add relevant indexes for common query patterns
            """,

        ["APIGenerator"] = """
            You are a Senior Backend Engineer specializing in REST API design.
            Your task: design and implement REST API endpoints.
            Output: C# minimal API endpoints with proper DTOs, validation, and error handling.
            Follow these rules:
            - Use proper HTTP verbs (GET/POST/PUT/DELETE)
            - Add input validation with FluentValidation
            - Return proper HTTP status codes
            - Add XML documentation comments
            - Use Result pattern for error handling
            """,

        ["UIComponents"] = """
            You are a Senior Frontend Engineer specializing in SolidJS.
            Your task: build UI components as specified.
            Output: SolidJS/TypeScript components with Tailwind CSS styling.
            Follow these rules:
            - Use SolidJS signals for state
            - Prefer createSignal over stores for local state
            - Use semantic HTML with proper ARIA attributes
            - Apply dark theme tokens from design system
            - Add loading and error states
            """,

        ["PipelineBuilder"] = """
            You are a Senior DevOps Engineer specializing in CI/CD.
            Your task: create a complete CI/CD pipeline configuration.
            Output: GitHub Actions YAML + Dockerfile(s) + docker-compose.yml.
            Follow these rules:
            - Multi-stage Docker builds for minimal image size
            - Run tests in CI before building
            - Add health checks to Docker services
            - Use secrets for sensitive configuration
            - Add deployment stages (dev, staging, prod)
            """,

        ["AuthSystem"] = """
            You are a Senior Security Engineer specializing in authentication.
            Your task: implement a complete authentication system.
            Output: JWT auth implementation with refresh tokens, roles, and middleware.
            Follow these rules:
            - Use HS256 or RS256 JWT signing
            - Implement refresh token rotation
            - Add role-based access control (RBAC)
            - Store passwords with Argon2id
            - Add rate limiting for auth endpoints
            """,

        ["VulnScanner"] = """
            You are a Senior Security Auditor.
            Your task: review the provided code for security vulnerabilities.
            Output: a structured security report with findings and fixes.
            Check for:
            - SQL injection
            - XSS vulnerabilities
            - Insecure direct object references (IDOR)
            - Missing authentication/authorization
            - Secrets hardcoded in code
            - Dependency vulnerabilities
            For each finding: severity (Critical/High/Medium/Low), location, fix.
            """,

        ["StateManager"] = """
            You are a Senior Frontend Architect specializing in state management.
            Your task: design and implement application state management.
            Output: SolidJS stores and signals with proper data flow.
            Follow these rules:
            - Use createStore for complex shared state
            - Use createSignal for local component state
            - Implement proper loading/error/success states
            - Add optimistic updates where appropriate
            - Document state shape with TypeScript interfaces
            """,
    };

    public SubagentDispatcher(IAIService ai, ILogger<SubagentDispatcher> logger)
    {
        _ai     = ai;
        _logger = logger;
    }

    public async Task<SubagentResult> DispatchAsync(
        string subagentType,
        string task,
        AgentContext context,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[SubagentDispatcher] Dispatching {SubagentType} for session {SessionId}",
            subagentType, context.SessionId);

        context.LogEvent(
            subagentType,
            $"Subagent {subagentType} started: {task[..Math.Min(80, task.Length)]}...");

        if (!SubagentPrompts.TryGetValue(subagentType, out var systemPrompt))
        {
            var error = $"Unknown subagent type: '{subagentType}'. " +
                        $"Available: {string.Join(", ", SubagentPrompts.Keys)}";
            _logger.LogWarning(error);
            return Fail(subagentType, error);
        }

        try
        {
            // Строим полный промпт с контекстом проекта
            var fullPrompt = BuildPrompt(task, context);
            var response   = await _ai.GenerateCompletionAsync(fullPrompt, systemPrompt, null);

            var result = new SubagentResult
            {
                SubagentType = subagentType,
                Content      = response,
                IsSuccess    = true,
            };

            // Сохраняем результат в контекст — сениор прочитает его оттуда
            context.SubagentResults[subagentType] = result;

            context.LogEvent(
                subagentType,
                $"Subagent {subagentType} completed",
                AgentContextEventType.Success);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SubagentDispatcher] {SubagentType} failed", subagentType);
            context.LogEvent(subagentType, $"Subagent {subagentType} failed: {ex.Message}",
                AgentContextEventType.Error);
            return Fail(subagentType, ex.Message);
        }
    }

    // ── Приватные методы ──────────────────────────────────────────────────

    private static string BuildPrompt(string task, AgentContext context)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"USER REQUEST: {context.UserPrompt}");
        sb.AppendLine($"TECH STACK: {context.Plan?.TechStack ?? "Not specified"}");
        sb.AppendLine($"PROJECT TYPE: {context.Plan?.ProjectType ?? "Not specified"}");

        // Добавляем результаты уже завершённых субагентов как контекст
        if (context.SubagentResults.Count > 0)
        {
            sb.AppendLine("\nALREADY COMPLETED BY OTHER SUBAGENTS:");
            foreach (var (type, result) in context.SubagentResults.Where(r => r.Value.IsSuccess))
            {
                sb.AppendLine($"--- {type} ---");
                // Берём только первые 500 символов чтобы не раздувать контекст
                sb.AppendLine(result.Content[..Math.Min(500, result.Content.Length)]);
            }
        }

        sb.AppendLine($"\nYOUR SPECIFIC TASK: {task}");
        return sb.ToString();
    }

    private static SubagentResult Fail(string type, string error) => new()
    {
        SubagentType = type,
        Content      = string.Empty,
        IsSuccess    = false,
        ErrorMessage = error,
    };
}
