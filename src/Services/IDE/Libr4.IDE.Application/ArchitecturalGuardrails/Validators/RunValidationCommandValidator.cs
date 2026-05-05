using FluentValidation;
using Libr4.IDE.Application.ArchitecturalGuardrails.Commands;

namespace Libr4.IDE.Application.ArchitecturalGuardrails.Validators;

/// <summary>
/// Validator for RunValidationCommand
/// </summary>
public class RunValidationCommandValidator : AbstractValidator<RunValidationCommand>
{
    public RunValidationCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required");
        
        RuleFor(x => x.Files)
            .NotEmpty()
            .WithMessage("At least one file is required")
            .Must(x => x.Count <= 100)
            .WithMessage("Cannot process more than 100 files");
    }
}
