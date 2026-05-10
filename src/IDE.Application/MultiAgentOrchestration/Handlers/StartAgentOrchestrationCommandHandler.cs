using Libr4.IDE.Application.MultiAgentOrchestration.Commands;
using Libr4.IDE.Application.MultiAgentOrchestration.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.MultiAgentOrchestration.Handlers;

/// <summary>
/// Обработчик команды запуска иерархической мульти-агентной оркестрации.
///
/// Этот handler — единственная точка входа в 4-уровневую систему агентов.
/// Он вызывается из MultiAgentOrchestrationEndpoints при POST /api/ide/orchestration/start.
///
/// Поток выполнения:
///   StartAgentOrchestrationCommand
///     → HierarchicalOrchestrationService.RunAsync()
///       → Level 1: LlmAppPlannerService   (план)
///       → Level 2: Orchestrator           (этот класс + HierarchicalOrchestrationService)
///       → Level 3: Senior Agents          (параллельно)
///       → Level 4: Subagents              (по запросу сениора)
///       → Quality Gate                    (retry если плохо)
///       → Final Assembly                  (финальный артефакт)
/// </summary>
public sealed class StartAgentOrchestrationCommandHandler
    : IRequestHandler<StartAgentOrchestrationCommand, AgentOrchestrationDto>
{
    private readonly HierarchicalOrchestrationService _orchestration;
    private readonly ILogger<StartAgentOrchestrationCommandHandler> _logger;

    public StartAgentOrchestrationCommandHandler(
        HierarchicalOrchestrationService orchestration,
        ILogger<StartAgentOrchestrationCommandHandler> logger)
    {
        _orchestration = orchestration;
        _logger        = logger;
    }

    public async Task<AgentOrchestrationDto> Handle(
        StartAgentOrchestrationCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[Orchestration] Starting for task: {Task}",
            request.MainTask[..Math.Min(100, request.MainTask.Length)]);

        var context = await _orchestration.RunAsync(
            userPrompt: request.MainTask,
            userId:     request.UserId,
            ct:         ct);

        // Маппим AgentContext → AgentOrchestrationDto для ответа клиенту
        return new AgentOrchestrationDto
        {
            Id          = context.SessionId,
            Task        = request.MainTask,
            Status      = context.IsCompleted ? "Completed" : "Failed",
            CreatedAt   = context.StartedAt,
            CompletedAt = DateTime.UtcNow,

            // Какие агенты участвовали
            Agents = context.SeniorOutputs.Keys.ToList(),

            // Краткий лог событий
            EventLog = context.Events
                .Select(e => $"[{e.OccurredAt:HH:mm:ss}] {e.AgentRole}: {e.Message}")
                .ToList(),

            // Финальный артефакт (полный код/структура проекта)
            FinalArtifact = context.FinalArtifact ?? string.Empty,

            // Статистика
            RetryCount   = context.RetryCount,
            TotalAgents  = context.SeniorOutputs.Count + context.SubagentResults.Count,
        };
    }
}
