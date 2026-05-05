using FluentValidation;
using Libr4.IDE.Application.OrchestrationRun.Commands;

namespace Libr4.IDE.Application.OrchestrationRun.Validators;

/// <summary>
/// Validator for StartOrchestrationRunCommand
/// </summary>
public class StartOrchestrationRunCommandValidator : AbstractValidator<StartOrchestrationRunCommand>
{
    public StartOrchestrationRunCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required");
        
        RuleFor(x => x.PhaseId)
            .NotEmpty()
            .WithMessage("Phase ID is required");
        
        RuleFor(x => x.PhaseName)
            .NotEmpty()
            .WithMessage("Phase name is required");
    }
}
