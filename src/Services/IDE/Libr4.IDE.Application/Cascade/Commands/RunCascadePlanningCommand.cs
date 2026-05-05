using MediatR;
using Libr4.IDE.Application.Cascade.DTOs;

namespace Libr4.IDE.Application.Cascade.Commands;

/// <summary>
/// Command to run cascade planning for orchestrator pass.
/// </summary>
public record RunCascadePlanningCommand : IRequest<OrchestratorPlanDto>
{
    public string Prompt { get; init; } = string.Empty;
    public string TaskDescription { get; init; } = string.Empty;
    public List<string> Subtasks { get; init; } = new();
    public string Complexity { get; init; } = "Medium";
    public bool PrefetchWeb { get; init; } = true;
}
