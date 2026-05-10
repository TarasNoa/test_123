using Libr4.IDE.Application.MultiAgentOrchestration.DTOs;
using MediatR;

namespace Libr4.IDE.Application.MultiAgentOrchestration.Commands;

/// <summary>
/// Команда запуска иерархической мульти-агентной оркестрации.
/// Отправляется из POST /api/ide/orchestration/start.
/// </summary>
public record StartAgentOrchestrationCommand : IRequest<AgentOrchestrationDto>
{
    /// <summary>Задача на естественном языке: "сгенерируй банковское приложение"</summary>
    public string MainTask { get; init; } = string.Empty;

    /// <summary>userId из JWT Claims — нужен для привязки сессии к пользователю</summary>
    public string UserId { get; init; } = string.Empty;

    // Устаревшие поля — оставляем для совместимости, но HierarchicalOrchestrationService их игнорирует
    public Guid         TaskId          { get; init; } = Guid.NewGuid();
    public List<string> AvailableAgents { get; init; } = new();
}
