/*
using Libr4.IDE.Application.MultiAgentOrchestration.Commands;
using Libr4.IDE.Application.MultiAgentOrchestration.DTOs;
using Libr4.IDE.Domain.MultiAgentOrchestration;
using Libr4.IDE.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.MultiAgentOrchestration.Handlers;

public class StartAgentOrchestrationCommandHandler : IRequestHandler<StartAgentOrchestrationCommand, AgentOrchestrationDto>
{
    private readonly IAgentOrchestrationRepository _orchestrationRepository;
    private readonly ILogger<StartAgentOrchestrationCommandHandler> _logger;

    public StartAgentOrchestrationCommandHandler(
        IAgentOrchestrationRepository orchestrationRepository,
        ILogger<StartAgentOrchestrationCommandHandler> logger)
    {
        _orchestrationRepository = orchestrationRepository;
        _logger = logger;
    }

    public async Task<AgentOrchestrationDto> Handle(StartAgentOrchestrationCommand request, CancellationToken ct)
    {
        var orchestration = AgentOrchestration.Create(
            request.Task,
            request.Agents,
            request.Strategy);

        await _orchestrationRepository.SaveAsync(orchestration, ct);

        _logger.LogInformation("Started agent orchestration {OrchestrationId} with {AgentCount} agents", orchestration.Id, request.Agents.Count);

        return new AgentOrchestrationDto
        {
            Id = orchestration.Id,
            Task = orchestration.Task,
            Agents = orchestration.Agents,
            Strategy = orchestration.Strategy,
            Status = orchestration.Status,
            CreatedAt = orchestration.CreatedAt
        };
    }
}
*/
