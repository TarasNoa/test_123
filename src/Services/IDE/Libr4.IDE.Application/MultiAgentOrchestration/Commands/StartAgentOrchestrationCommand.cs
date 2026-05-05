using MediatR;
using Libr4.IDE.Application.MultiAgentOrchestration.DTOs;

namespace Libr4.IDE.Application.MultiAgentOrchestration.Commands;

/// <summary>
/// Command to start multi-agent orchestration
/// </summary>
public record StartAgentOrchestrationCommand : IRequest<AgentOrchestrationDto>
{
    public Guid TaskId { get; init; }
    public string MainTask { get; init; } = string.Empty;
    public List<string> AvailableAgents { get; init; } = new();
}
