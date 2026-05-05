using FluentValidation;
using Libr4.IDE.Application.SemanticCodeGraph.Commands;

namespace Libr4.IDE.Application.SemanticCodeGraph.Validators;

/// <summary>
/// Validator for BuildGraphCommand
/// </summary>
public class BuildGraphCommandValidator : AbstractValidator<BuildGraphCommand>
{
    public BuildGraphCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required");
        
        RuleFor(x => x.Files)
            .NotEmpty()
            .WithMessage("At least one file is required")
            .Must(x => x.Count <= 100)
            .WithMessage("Cannot process more than 100 files");
        
        RuleForEach(x => x.Files)
            .Must(x => !string.IsNullOrWhiteSpace(x.FilePath))
            .WithMessage("File path cannot be empty");
    }
}
