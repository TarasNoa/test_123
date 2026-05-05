using FluentValidation;
using Libr4.IDE.Application.SeniorRolePrompts.Commands;

namespace Libr4.IDE.Application.SeniorRolePrompts.Validators;

/// <summary>
/// Validator for GenerateRolePromptCommand
/// </summary>
public class GenerateRolePromptCommandValidator : AbstractValidator<GenerateRolePromptCommand>
{
    public GenerateRolePromptCommandValidator()
    {
        RuleFor(x => x.PhaseType)
            .IsInEnum()
            .WithMessage("Invalid phase type");
        
        RuleFor(x => x.PhaseName)
            .NotEmpty()
            .WithMessage("Phase name is required")
            .MaximumLength(200)
            .WithMessage("Phase name must not exceed 200 characters");
        
        RuleFor(x => x.DomainClass)
            .Must(x => new[] { "Standard", "Regulated", "SafetyCritical" }.Contains(x))
            .WithMessage("Domain class must be Standard, Regulated, or SafetyCritical");
    }
}
