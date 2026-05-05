using FluentValidation;
using Libr4.IDE.Application.HackerAgent.Commands;

namespace Libr4.IDE.Application.HackerAgent.Validators;

/// <summary>
/// Validator for RunHackerAgentCommand
/// </summary>
public class RunHackerAgentCommandValidator : AbstractValidator<RunHackerAgentCommand>
{
    public RunHackerAgentCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required");
        
        RuleFor(x => x.Target)
            .NotEmpty()
            .WithMessage("Target is required")
            .Must(x => Uri.TryCreate(x, UriKind.Absolute, out _))
            .WithMessage("Target must be a valid URL");
    }
}
