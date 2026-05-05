using FluentValidation;
using Libr4.IDE.Application.ShadowWorkspace.Commands;

namespace Libr4.IDE.Application.ShadowWorkspace.Validators;

/// <summary>
/// Validator for CreateShadowWorkspaceCommand
/// </summary>
public class CreateShadowWorkspaceCommandValidator : AbstractValidator<CreateShadowWorkspaceCommand>
{
    public CreateShadowWorkspaceCommandValidator()
    {
        RuleFor(x => x.ParentWorkspaceId)
            .NotEmpty()
            .WithMessage("Parent workspace ID is required");
        
        RuleFor(x => x.Files)
            .Must(x => x.Count <= 100)
            .WithMessage("Cannot process more than 100 files");
        
        RuleForEach(x => x.Files)
            .Must(x => !string.IsNullOrWhiteSpace(x.FilePath))
            .WithMessage("File path cannot be empty");
    }
}
