namespace Libr4.IDE.Application.MultiAgentOrchestration.DTOs;

/// <summary>
/// DTO результата мульти-агентной оркестрации.
/// Возвращается клиенту после завершения генерации.
/// </summary>
public sealed class AgentOrchestrationDto
{
    public Guid   Id          { get; init; }
    public string Task        { get; init; } = string.Empty;
    public string Status      { get; init; } = string.Empty;  // "Completed" | "Failed"
    public DateTime CreatedAt { get; init; }
    public DateTime CompletedAt { get; init; }

    /// <summary>Какие агенты участвовали в генерации</summary>
    public List<string> Agents { get; init; } = new();

    /// <summary>Лог событий для отображения в UI</summary>
    public List<string> EventLog { get; init; } = new();

    /// <summary>Финальный результат — полный код/структура проекта</summary>
    public string FinalArtifact { get; init; } = string.Empty;

    /// <summary>Сколько retry-циклов было выполнено</summary>
    public int RetryCount { get; init; }

    /// <summary>Общее количество агентов (сениоров + субагентов)</summary>
    public int TotalAgents { get; init; }
}
