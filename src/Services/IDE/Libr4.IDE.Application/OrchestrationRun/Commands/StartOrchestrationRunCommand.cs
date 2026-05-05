using MediatR;
using Libr4.IDE.Application.OrchestrationRun.DTOs;

namespace Libr4.IDE.Application.OrchestrationRun.Commands;

/// <summary>
/// Command to start an orchestration run
/// </summary>
public record StartOrchestrationRunCommand : IRequest<OrchestrationRunDto>
{
    public string TaskId { get; init; } = string.Empty;
    public string PhaseId { get; init; } = string.Empty;
    public string PhaseName { get; init; } = string.Empty;
}
