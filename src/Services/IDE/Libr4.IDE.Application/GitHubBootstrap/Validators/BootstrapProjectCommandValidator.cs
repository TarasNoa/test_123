using FluentValidation;
using Libr4.IDE.Application.GitHubBootstrap.Commands;

namespace Libr4.IDE.Application.GitHubBootstrap.Validators;

/// <summary>
/// Validator for BootstrapProjectCommand
/// </summary>
public class BootstrapProjectCommandValidator : AbstractValidator<BootstrapProjectCommand>
{
    public BootstrapProjectCommandValidator()
    {
        RuleFor(x => x.ProjectName)
            .NotEmpty()
            .WithMessage("Project name is required")
            .MaximumLength(100)
            .WithMessage("Project name must not exceed 100 characters");
        
        RuleFor(x => x.Language)
            .NotEmpty()
            .WithMessage("Language is required");
        
        RuleFor(x => x.AllowedLicenses)
            .NotEmpty()
            .WithMessage("At least one allowed license must be specified");
    }
}
