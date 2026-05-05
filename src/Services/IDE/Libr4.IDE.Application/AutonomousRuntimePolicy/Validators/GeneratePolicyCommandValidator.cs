using FluentValidation;
using Libr4.IDE.Application.AutonomousRuntimePolicy.Commands;

namespace Libr4.IDE.Application.AutonomousRuntimePolicy.Validators;

/// <summary>
/// Validator for GeneratePolicyCommand
/// </summary>
public class GeneratePolicyCommandValidator : AbstractValidator<GeneratePolicyCommand>
{
    public GeneratePolicyCommandValidator()
    {
        RuleFor(x => x.Prompt)
            .NotEmpty()
            .WithMessage("Prompt is required")
            .MinimumLength(10)
            .WithMessage("Prompt must be at least 10 characters")
            .MaximumLength(10000)
            .WithMessage("Prompt must not exceed 10000 characters");
        
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required");
    }
}
