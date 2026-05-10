using Libr4.AI.Infrastructure.AI;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Libr4.IDE.Application.MultiAgentOrchestration;

/// <summary>
/// Главный сервис иерархической мульти-агентной системы.
///
/// Реализует полный 4-уровневый pipeline:
///
///   Уровень 1: Planner Agent
///              ↓ GenerationPlan
///   Уровень 2: Orchestrator (этот класс)
///              ↓ задачи по ролям
///   Уровень 3: Senior Agents (параллельно)
///              ↓ вызывают субагентов через ISubagentDispatcher
///   Уровень 4: Subagents (по запросу сениора)
///              ↓ результаты накапливаются в AgentContext
///   Quality Gate → если не прошло: retry конкретного сениора
///              ↓ прошло
///   Финальная сборка → готовое приложение
/// </summary>
public sealed class HierarchicalOrchestrationService
{
    private readonly IAppPlannerService    _planner;
    private readonly IAIService            _ai;
    private readonly ISubagentDispatcher   _subagentDispatcher;
    private readonly IQualityGateService   _qualityGate;
    private readonly ILogger<HierarchicalOrchestrationService> _logger;

    // Системные промпты сениор-агентов
    // Каждый сениор знает о своей роли и умеет вызывать субагентов
    private static readonly Dictionary<string, string> SeniorPrompts = new()
    {
        ["SeniorBackend"] = """
            You are a Senior Backend Developer (Lead).
            You design the overall backend architecture and coordinate specialized subagents.
            Your responsibilities:
            - Design the service architecture
            - Specify the database schema (delegate to DBDesigner subagent)
            - Design the REST API (delegate to APIGenerator subagent)
            - Implement the business logic
            - Set up authentication (delegate to AuthSystem subagent if needed)
            
            Available subagents you can request: DBDesigner, APIGenerator, AuthSystem
            
            Output: Complete backend implementation plan + code structure.
            """,

        ["SeniorFrontend"] = """
            You are a Senior Frontend Developer (Lead).
            You design the UI architecture and coordinate specialized subagents.
            Your responsibilities:
            - Design the component hierarchy and routing
            - Build core UI components (delegate complex ones to UIComponents subagent)
            - Design state management (delegate to StateManager subagent)
            - Ensure design system consistency
            
            Available subagents you can request: UIComponents, StateManager
            
            Output: Complete frontend implementation plan + component structure.
            """,

        ["SeniorCICD"] = """
            You are a Senior DevOps Engineer (Lead).
            You design the complete CI/CD infrastructure.
            Your responsibilities:
            - Design the deployment pipeline
            - Create Docker configurations (delegate to PipelineBuilder subagent)
            - Set up environment configurations
            - Design monitoring and alerting
            
            Available subagents you can request: PipelineBuilder
            
            Output: Complete CI/CD pipeline + Docker configuration.
            """,

        ["SeniorSecurity"] = """
            You are a Senior Security Engineer (Lead).
            You ensure the entire application is secure.
            Your responsibilities:
            - Review all architecture decisions for security issues
            - Audit generated code (delegate to VulnScanner subagent)
            - Implement authentication strategy (coordinate with SeniorBackend via AuthSystem)
            - Security checklist and threat model
            
            Available subagents you can request: VulnScanner, AuthSystem
            
            Output: Security audit report + threat model + fixes.
            """,
    };

    // Маппинг: роль из плана → какие субагенты нужно вызвать автоматически
    private static readonly Dictionary<string, string[]> DefaultSubagentsForRole = new()
    {
        ["SeniorBackend"]  = ["DBDesigner", "APIGenerator"],
        ["SeniorFrontend"] = ["UIComponents", "StateManager"],
        ["SeniorCICD"]     = ["PipelineBuilder"],
        ["SeniorSecurity"] = ["VulnScanner"],
    };

    public HierarchicalOrchestrationService(
        IAppPlannerService  planner,
        IAIService          ai,
        ISubagentDispatcher subagentDispatcher,
        IQualityGateService qualityGate,
        ILogger<HierarchicalOrchestrationService> logger)
    {
        _planner            = planner;
        _ai                 = ai;
        _subagentDispatcher = subagentDispatcher;
        _qualityGate        = qualityGate;
        _logger             = logger;
    }

    // ── Точка входа ──────────────────────────────────────────────────────────

    /// <summary>
    /// Запускает полный pipeline генерации приложения.
    /// Вызывается из StartAgentOrchestrationCommandHandler.
    /// </summary>
    public async Task<AgentContext> RunAsync(
        string userPrompt,
        string userId,
        CancellationToken ct = default)
    {
        var context = new AgentContext
        {
            UserId     = userId,
            UserPrompt = userPrompt,
        };

        _logger.LogInformation(
            "[Hierarchy] Starting session {SessionId} for user {UserId}",
            context.SessionId, userId);

        try
        {
            // ── Уровень 1: Planner ─────────────────────────────────────────
            context.LogEvent("Planner", "Analyzing request and building generation plan...");
            await RunPlannerAsync(context, ct);

            // ── Retry loop ─────────────────────────────────────────────────
            while (!context.IsCompleted && context.CanRetry)
            {
                context.RetryCount++;

                // ── Уровень 3+4: Senior Agents + Subagents ─────────────────
                await RunSeniorAgentsAsync(context, ct);

                // ── Quality Gate ───────────────────────────────────────────
                context.LogEvent("QualityGate", $"Running quality check (attempt {context.RetryCount})...");
                var passed = await RunQualityGateAsync(context, ct);

                if (passed)
                {
                    // ── Финальная сборка ───────────────────────────────────
                    context.LogEvent("Assembler", "All quality checks passed. Assembling final output...");
                    await AssembleFinalArtifactAsync(context, ct);
                    context.IsCompleted = true;
                }
                else
                {
                    _logger.LogWarning(
                        "[Hierarchy] Quality gate failed. Retry {Count}/{Max}. Feedback: {Count2} issues",
                        context.RetryCount, AgentContext.MaxRetries, context.QualityFeedback.Count);
                }
            }

            if (!context.IsCompleted)
            {
                context.LogEvent("Orchestrator",
                    $"Max retries ({AgentContext.MaxRetries}) reached. Using best available output.",
                    AgentContextEventType.Warning);
                // Используем лучший из имеющихся результатов без финальной сборки
                await AssembleFinalArtifactAsync(context, ct);
                context.IsCompleted = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Hierarchy] Session {SessionId} failed", context.SessionId);
            context.LogEvent("Orchestrator", $"Fatal error: {ex.Message}", AgentContextEventType.Error);
        }

        return context;
    }

    // ── Уровень 1: Planner ───────────────────────────────────────────────────

    private async Task RunPlannerAsync(AgentContext context, CancellationToken ct)
    {
        try
        {
            // LlmAppPlannerService уже реализован — используем его
            var rawPlan = await _planner.CreatePlanAsync(context.UserPrompt, ct);

            // Парсим план и определяем нужных сениоров
            context.Plan = ParsePlan(rawPlan, context.UserPrompt);

            context.LogEvent("Planner",
                $"Plan created: {context.Plan.ProjectType}, roles: {string.Join(", ", context.Plan.RequiredSeniorRoles)}",
                AgentContextEventType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Planner] Failed to parse LLM plan, using default");
            context.Plan = BuildDefaultPlan(context.UserPrompt);
            context.LogEvent("Planner", "Using default plan (LLM plan parse failed)", AgentContextEventType.Warning);
        }
    }

    // ── Уровень 3+4: Senior Agents параллельно ───────────────────────────────

    private async Task RunSeniorAgentsAsync(AgentContext context, CancellationToken ct)
    {
        var plan = context.Plan!;

        // Разбиваем фазы на волны по зависимостям (топологическая сортировка)
        var waves = BuildExecutionWaves(plan.Phases);

        foreach (var wave in waves)
        {
            // Фазы внутри волны — параллельно
            var tasks = wave.Select(phase => RunSeniorAgentAsync(phase, context, ct));
            await Task.WhenAll(tasks);
        }
    }

    private async Task RunSeniorAgentAsync(PlanPhase phase, AgentContext context, CancellationToken ct)
    {
        var role = phase.AssignedRole;
        context.LogEvent(role, $"Starting: {phase.Description}");

        try
        {
            // Проверяем — нужна ли переработка этого агента (фидбек от Quality Gate)
            var feedback = context.QualityFeedback.FirstOrDefault(f => f.AgentRole == role);
            var task     = feedback != null
                ? $"REWORK NEEDED: {feedback.FailureReason}\nRECOMMENDATION: {feedback.Recommendation}\n\nOriginal task: {phase.Description}"
                : phase.Description;

            // 1. Запускаем субагентов этого сениора параллельно
            if (DefaultSubagentsForRole.TryGetValue(role, out var subagentTypes))
            {
                var subagentTasks = subagentTypes.Select(type =>
                    _subagentDispatcher.DispatchAsync(type, $"{task}\n\nFocus on your specialty.", context, ct));
                await Task.WhenAll(subagentTasks);
            }

            // 2. Сениор синтезирует результаты субагентов в финальный артефакт
            var systemPrompt = SeniorPrompts.GetValueOrDefault(role,
                $"You are a {role}. Complete the assigned task professionally.");

            var fullPrompt = BuildSeniorPrompt(task, role, context);
            var content    = await _ai.GenerateAsync(systemPrompt, fullPrompt, ct);

            context.SeniorOutputs[role] = new SeniorOutput
            {
                AgentRole = role,
                Content   = content,
                IsSuccess = true,
            };

            context.LogEvent(role, $"Completed successfully", AgentContextEventType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Senior] {Role} failed", role);
            context.SeniorOutputs[role] = new SeniorOutput
            {
                AgentRole    = role,
                Content      = string.Empty,
                IsSuccess    = false,
                ErrorMessage = ex.Message,
            };
            context.LogEvent(role, $"Failed: {ex.Message}", AgentContextEventType.Error);
        }
    }

    // ── Quality Gate ─────────────────────────────────────────────────────────

    private async Task<bool> RunQualityGateAsync(AgentContext context, CancellationToken ct)
    {
        context.QualityFeedback.Clear();

        // Системный промпт для ревьюера — LLM оценивает весь вывод сениоров
        const string reviewerPrompt = """
            You are a Principal Engineer reviewing output from multiple AI agents.
            Your job: evaluate the QUALITY and COMPLETENESS of their output.
            
            For each agent output, decide:
            - PASS: Output is complete, correct, and production-ready
            - FAIL: Output has issues that must be fixed
            
            Respond in JSON:
            {
              "passed": true/false,
              "feedback": [
                {
                  "agentRole": "SeniorBackend",
                  "status": "PASS" | "FAIL",
                  "failureReason": "...",
                  "recommendation": "specific fix needed"
                }
              ]
            }
            """;

        var summary = BuildQualityReviewInput(context);

        try
        {
            var response = await _ai.GenerateAsync(reviewerPrompt, summary, ct);
            var review   = ParseQualityReview(response);

            foreach (var item in review.Feedback.Where(f => f.Status == "FAIL"))
            {
                context.QualityFeedback.Add(new QualityFeedback
                {
                    AgentRole      = item.AgentRole,
                    FailureReason  = item.FailureReason,
                    Recommendation = item.Recommendation,
                });
            }

            if (!review.Passed)
            {
                context.LogEvent("QualityGate",
                    $"Failed: {context.QualityFeedback.Count} issues found",
                    AgentContextEventType.Warning);
            }

            return review.Passed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[QualityGate] Review parse failed, passing anyway");
            return true; // при ошибке парсинга — не блокируем
        }
    }

    // ── Финальная сборка ─────────────────────────────────────────────────────

    private async Task AssembleFinalArtifactAsync(AgentContext context, CancellationToken ct)
    {
        const string assemblerPrompt = """
            You are a Technical Lead assembling the final application from components built by multiple agents.
            Combine all provided components into a coherent, complete project structure.
            Output a clear, organized project structure with all files and their content.
            """;

        var allOutputs = string.Join("\n\n---\n\n",
            context.SeniorOutputs.Values
                .Where(o => o.IsSuccess)
                .Select(o => $"### {o.AgentRole}\n{o.Content}"));

        var prompt = $"PROJECT: {context.UserPrompt}\n\nCOMPONENTS:\n{allOutputs}";

        context.FinalArtifact = await _ai.GenerateAsync(assemblerPrompt, prompt, ct);
    }

    // ── Вспомогательные методы ────────────────────────────────────────────────

    private static string BuildSeniorPrompt(string task, string role, AgentContext context)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"PROJECT: {context.UserPrompt}");
        sb.AppendLine($"TECH STACK: {context.Plan?.TechStack}");
        sb.AppendLine($"YOUR ROLE: {role}");
        sb.AppendLine($"YOUR TASK: {task}");

        // Добавляем результаты субагентов которые уже отработали
        var relevantSubagents = DefaultSubagentsForRole.GetValueOrDefault(role, []);
        var subagentOutputs = context.SubagentResults
            .Where(r => relevantSubagents.Contains(r.Key) && r.Value.IsSuccess)
            .ToList();

        if (subagentOutputs.Count > 0)
        {
            sb.AppendLine("\nSUBAGENT RESULTS TO INCORPORATE:");
            foreach (var (type, result) in subagentOutputs)
            {
                sb.AppendLine($"--- {type} ---");
                sb.AppendLine(result.Content);
            }
        }

        sb.AppendLine("\nSynthesize the above into your final output.");
        return sb.ToString();
    }

    private static string BuildQualityReviewInput(AgentContext context)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"PROJECT REQUEST: {context.UserPrompt}");
        sb.AppendLine($"AGENT OUTPUTS TO REVIEW:");

        foreach (var (role, output) in context.SeniorOutputs)
        {
            sb.AppendLine($"\n=== {role} ===");
            sb.AppendLine(output.IsSuccess ? output.Content : $"FAILED: {output.ErrorMessage}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Строит волны выполнения по зависимостям.
    /// Фазы без зависимостей (или с выполненными) идут в одной волне — параллельно.
    /// </summary>
    private static List<List<PlanPhase>> BuildExecutionWaves(List<PlanPhase> phases)
    {
        var waves     = new List<List<PlanPhase>>();
        var completed = new HashSet<string>();
        var remaining = phases.ToList();

        while (remaining.Count > 0)
        {
            var wave = remaining
                .Where(p => p.Dependencies.All(d => completed.Contains(d)))
                .ToList();

            if (wave.Count == 0)
            {
                // Циклическая зависимость или ошибка — добавляем все оставшиеся
                waves.Add(remaining);
                break;
            }

            waves.Add(wave);
            wave.ForEach(p => { completed.Add(p.Name); remaining.Remove(p); });
        }

        return waves;
    }

    private static GenerationPlan ParsePlan(string rawPlanJson, string userPrompt)
    {
        try
        {
            var doc   = JsonDocument.Parse(rawPlanJson);
            var root  = doc.RootElement;
            var roles = new List<string> { "SeniorBackend", "SeniorFrontend" };

            // Читаем phases из JSON если есть
            var phases = new List<PlanPhase>();
            if (root.TryGetProperty("phases", out var phasesEl))
            {
                foreach (var p in phasesEl.EnumerateArray())
                {
                    phases.Add(new PlanPhase
                    {
                        Name         = p.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        AssignedRole = p.TryGetProperty("agent", out var a) ? a.GetString() ?? "SeniorBackend" : "SeniorBackend",
                        Description  = p.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                        Dependencies = p.TryGetProperty("depends_on", out var dep)
                            ? dep.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                            : new List<string>(),
                    });
                }
            }

            // Если фаз нет — строим дефолтный план
            if (phases.Count == 0)
                return BuildDefaultPlan(userPrompt);

            return new GenerationPlan
            {
                ProjectType          = root.TryGetProperty("type", out var t) ? t.GetString() ?? "webapp" : "webapp",
                TechStack            = root.TryGetProperty("stack", out var s) ? s.GetString() ?? "" : "",
                RequiredSeniorRoles  = roles,
                Phases               = phases,
                RawPlanJson          = rawPlanJson,
            };
        }
        catch
        {
            return BuildDefaultPlan(userPrompt);
        }
    }

    private static GenerationPlan BuildDefaultPlan(string userPrompt) => new()
    {
        ProjectType         = "webapp",
        TechStack           = "SolidJS + .NET 8 + PostgreSQL",
        RequiredSeniorRoles = ["SeniorBackend", "SeniorFrontend", "SeniorCICD", "SeniorSecurity"],
        Phases =
        [
            new() { Name = "backend",  AssignedRole = "SeniorBackend",  Description = $"Build backend for: {userPrompt}", Dependencies = [],            CanRunInParallel = false },
            new() { Name = "frontend", AssignedRole = "SeniorFrontend", Description = $"Build frontend for: {userPrompt}", Dependencies = [],           CanRunInParallel = true  },
            new() { Name = "cicd",     AssignedRole = "SeniorCICD",     Description = "Create CI/CD pipeline",             Dependencies = ["backend"],  CanRunInParallel = false },
            new() { Name = "security", AssignedRole = "SeniorSecurity", Description = "Security audit",                    Dependencies = ["backend", "frontend"], CanRunInParallel = false },
        ],
    };

    // ── Внутренние DTO для Quality Gate JSON ─────────────────────────────────

    private static QualityReviewResult ParseQualityReview(string json)
    {
        var clean = json.Contains('{') ? json[json.IndexOf('{')..(json.LastIndexOf('}') + 1)] : json;
        var doc   = JsonDocument.Parse(clean);
        var root  = doc.RootElement;

        var passed   = root.TryGetProperty("passed", out var p) && p.GetBoolean();
        var feedback = new List<QualityReviewItem>();

        if (root.TryGetProperty("feedback", out var fbArr))
        {
            foreach (var item in fbArr.EnumerateArray())
            {
                feedback.Add(new QualityReviewItem(
                    item.TryGetProperty("agentRole",       out var r) ? r.GetString() ?? "" : "",
                    item.TryGetProperty("status",          out var s) ? s.GetString() ?? "PASS" : "PASS",
                    item.TryGetProperty("failureReason",   out var f) ? f.GetString() ?? "" : "",
                    item.TryGetProperty("recommendation",  out var rec) ? rec.GetString() ?? "" : ""
                ));
            }
        }

        return new QualityReviewResult(passed, feedback);
    }

    private record QualityReviewResult(bool Passed, List<QualityReviewItem> Feedback);
    private record QualityReviewItem(string AgentRole, string Status, string FailureReason, string Recommendation);
}
