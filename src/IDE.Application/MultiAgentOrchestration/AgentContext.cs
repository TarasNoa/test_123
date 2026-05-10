namespace Libr4.IDE.Application.MultiAgentOrchestration;

/// <summary>
/// Общий контекст, который путешествует через всю иерархию агентов:
///   Planner → Orchestrator → Senior Agents → Subagents → Quality Gate
///
/// Накапливает артефакты, результаты и обратную связь по всей цепочке.
/// Это единственное место, где хранится "рабочая память" текущей генерации.
/// </summary>
public sealed class AgentContext
{
    // ── Идентификация ──────────────────────────────────────────────────────
    public Guid   SessionId  { get; init; } = Guid.NewGuid();
    public string UserId     { get; init; } = string.Empty;
    public string UserPrompt { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    // ── Результат планировщика ─────────────────────────────────────────────
    /// Заполняется Planner Agent на уровне 1
    public GenerationPlan? Plan { get; set; }

    // ── Артефакты от сениор-агентов ────────────────────────────────────────
    /// Ключ = роль агента ("SeniorBackend", "SeniorFrontend", ...)
    /// Значение = сгенерированный код / конфиг / документ
    public Dictionary<string, SeniorOutput> SeniorOutputs { get; } = new();

    // ── Артефакты от субагентов ────────────────────────────────────────────
    /// Ключ = тип субагента ("DBDesigner", "APIGenerator", ...)
    public Dictionary<string, SubagentResult> SubagentResults { get; } = new();

    // ── Quality Gate feedback ──────────────────────────────────────────────
    /// Заполняется после каждого прохода Quality Gate
    /// Содержит какие агенты должны переделать свою часть
    public List<QualityFeedback> QualityFeedback { get; } = new();

    // ── Retry логика ───────────────────────────────────────────────────────
    public int RetryCount { get; set; }
    public const int MaxRetries = 3;
    public bool CanRetry => RetryCount < MaxRetries;

    // ── Финальный результат ────────────────────────────────────────────────
    public string? FinalArtifact { get; set; }
    public bool IsCompleted { get; set; }

    // ── Лог событий (для SignalR стриминга в UI) ───────────────────────────
    public List<AgentContextEvent> Events { get; } = new();

    public void LogEvent(string agentRole, string message, AgentContextEventType type = AgentContextEventType.Info)
    {
        Events.Add(new AgentContextEvent
        {
            AgentRole  = agentRole,
            Message    = message,
            Type       = type,
            OccurredAt = DateTime.UtcNow
        });
    }
}

// ── Вспомогательные типы ────────────────────────────────────────────────────

/// <summary>
/// Результат одного сениор-агента
/// </summary>
public sealed class SeniorOutput
{
    public string AgentRole  { get; init; } = string.Empty;
    public string Content    { get; init; } = string.Empty;  // код, конфиг, doc
    public bool   IsSuccess  { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Результат субагента вызванного сениором
/// </summary>
public sealed class SubagentResult
{
    public string SubagentType { get; init; } = string.Empty;
    public string Content      { get; init; } = string.Empty;
    public bool   IsSuccess    { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Фидбек от Quality Gate — говорит какой агент должен переделать и почему
/// </summary>
public sealed class QualityFeedback
{
    public string AgentRole      { get; init; } = string.Empty;
    public string FailureReason  { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

/// <summary>
/// Событие в контексте — для логирования и SignalR стриминга
/// </summary>
public sealed class AgentContextEvent
{
    public string AgentRole    { get; init; } = string.Empty;
    public string Message      { get; init; } = string.Empty;
    public AgentContextEventType Type { get; init; }
    public DateTime OccurredAt { get; init; }
}

public enum AgentContextEventType { Info, Warning, Error, Success }

/// <summary>
/// План генерации — результат Planner Agent
/// </summary>
public sealed class GenerationPlan
{
    public string ProjectType { get; init; } = string.Empty;
    public string TechStack   { get; init; } = string.Empty;

    /// Какие сениор-агенты нужны для этого проекта
    public List<string> RequiredSeniorRoles { get; init; } = new();

    /// Фазы в порядке выполнения (с зависимостями)
    public List<PlanPhase> Phases { get; init; } = new();

    public string RawPlanJson { get; init; } = string.Empty;
}

public sealed class PlanPhase
{
    public string       Name         { get; init; } = string.Empty;
    public string       AssignedRole { get; init; } = string.Empty;  // "SeniorBackend" и т.д.
    public string       Description  { get; init; } = string.Empty;
    public List<string> Dependencies { get; init; } = new();          // фазы которые должны завершиться раньше
    public bool         CanRunInParallel { get; init; }
}
