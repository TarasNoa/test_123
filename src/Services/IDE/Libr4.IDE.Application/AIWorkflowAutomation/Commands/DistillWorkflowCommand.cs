using MediatR;
using Libr4.IDE.Application.AIWorkflowAutomation.DTOs;

namespace Libr4.IDE.Application.AIWorkflowAutomation.Commands;

/// <summary>
/// Command to distill workflow into skill
/// </summary>
public record DistillWorkflowCommand : IRequest<WorkflowAnalysisDto>
{
    public string WorkflowId { get; init; } = string.Empty;
    public List<string> WorkflowSteps { get; init; } = new();
}
