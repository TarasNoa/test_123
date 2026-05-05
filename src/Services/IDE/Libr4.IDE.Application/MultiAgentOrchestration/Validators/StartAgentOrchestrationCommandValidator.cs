using FluentValidation;
using Libr4.IDE.Application.MultiAgentOrchestration.Commands;

namespace Libr4.IDE.Application.MultiAgentOrchestration.Validators;

/// <summary>
/// Validator for StartAgentOrchestrationCommand
/// </summary>
public class StartAgentOrchestrationCommandValidator : AbstractValidator<StartAgentOrchestrationCommand>
{
    public StartAgentOrchestrationCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required");
        
        RuleFor(x => x.MainTask)
            .NotNull()
            .WithMessage("Main task is required");
        
        RuleFor(x => x.AvailableAgents)
            .NotEmpty()
            .WithMessage("At least one agent must be available");
    }
}
