using FluentValidation;
using Libr4.IDE.Application.IntelligenceRouter.Commands;

namespace Libr4.IDE.Application.IntelligenceRouter.Validators;

/// <summary>
/// Validator for BuildRoutingPlanCommand
/// </summary>
public class BuildRoutingPlanCommandValidator : AbstractValidator<BuildRoutingPlanCommand>
{
    public BuildRoutingPlanCommandValidator()
    {
        RuleFor(x => x.Prompt)
            .NotEmpty()
            .WithMessage("Prompt is required")
            .MinimumLength(10)
            .WithMessage("Prompt must be at least 10 characters")
            .MaximumLength(10000)
            .WithMessage("Prompt must not exceed 10000 characters");
        
        RuleFor(x => x.DomainClass)
            .Must(x => new[] { "Standard", "Regulated", "SafetyCritical" }.Contains(x))
            .WithMessage("Domain class must be Standard, Regulated, or SafetyCritical");
        
        RuleFor(x => x.RiskLevel)
            .Must(x => new[] { "low", "medium", "high", "critical" }.Contains(x.ToLower()))
            .WithMessage("Risk level must be low, medium, high, or critical");
        
        RuleFor(x => x.ContextFiles)
            .Must(x => x.Count <= 100)
            .WithMessage("Cannot process more than 100 context files");
    }
}
