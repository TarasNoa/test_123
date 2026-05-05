using FluentValidation;
using Libr4.IDE.Application.SemanticBlame.Commands;

namespace Libr4.IDE.Application.SemanticBlame.Validators;

/// <summary>
/// Validator for RunBlameCommand
/// </summary>
public class RunBlameCommandValidator : AbstractValidator<RunBlameCommand>
{
    public RunBlameCommandValidator()
    {
        RuleFor(x => x.FilePath)
            .NotEmpty()
            .WithMessage("File path is required");
        
        RuleFor(x => x.WorkspacePath)
            .NotEmpty()
            .WithMessage("Workspace path is required");
    }
}
