using FluentValidation;
using Libr4.IDE.Application.AIWorkflowAutomation.Commands;

namespace Libr4.IDE.Application.AIWorkflowAutomation.Validators;

/// <summary>
/// Validator for DistillWorkflowCommand
/// </summary>
public class DistillWorkflowCommandValidator : AbstractValidator<DistillWorkflowCommand>
{
    public DistillWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowId)
            .NotEmpty()
            .WithMessage("Workflow ID is required");
        
        RuleFor(x => x.WorkflowSteps)
            .NotEmpty()
            .WithMessage("At least one workflow step is required")
            .Must(x => x.Count <= 50)
            .WithMessage("Cannot process more than 50 steps");
    }
}
