using FluentValidation;
using Libr4.IDE.Application.CodeReview.Commands;

namespace Libr4.IDE.Application.CodeReview.Validators;

/// <summary>
/// Validator for RunCodeReviewCommand
/// </summary>
public class RunCodeReviewCommandValidator : AbstractValidator<RunCodeReviewCommand>
{
    public RunCodeReviewCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required");
        
        RuleFor(x => x.Files)
            .NotEmpty()
            .WithMessage("At least one file is required")
            .Must(x => x.Count <= 100)
            .WithMessage("Cannot review more than 100 files");
        
        RuleFor(x => x.ReviewTypes)
            .NotEmpty()
            .WithMessage("At least one review type is required");
    }
}
