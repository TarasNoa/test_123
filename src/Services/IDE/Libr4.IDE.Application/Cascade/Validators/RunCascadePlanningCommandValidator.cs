using FluentValidation;
using Libr4.IDE.Application.Cascade.Commands;

namespace Libr4.IDE.Application.Cascade.Validators;

/// <summary>
/// Validator for RunCascadePlanningCommand
/// </summary>
public class RunCascadePlanningCommandValidator : AbstractValidator<RunCascadePlanningCommand>
{
    public RunCascadePlanningCommandValidator()
    {
        RuleFor(x => x.Prompt)
            .NotEmpty()
            .WithMessage("Prompt is required")
            .MinimumLength(10)
            .WithMessage("Prompt must be at least 10 characters")
            .MaximumLength(10000)
            .WithMessage("Prompt must not exceed 10000 characters");

        RuleFor(x => x.Complexity)
            .Must(c => c is "Low" or "Medium" or "High" or "Critical")
            .WithMessage("Complexity must be Low, Medium, High, or Critical");
    }
}
