using FluentValidation;
using Libr4.IDE.Application.CodeIntelligence.Commands;

namespace Libr4.IDE.Application.CodeIntelligence.Validators;

/// <summary>
/// Validator for GetCompletionsCommand
/// </summary>
public class GetCompletionsCommandValidator : AbstractValidator<GetCompletionsCommand>
{
    public GetCompletionsCommandValidator()
    {
        RuleFor(x => x.FilePath)
            .NotEmpty()
            .WithMessage("File path is required");
        
        RuleFor(x => x.Line)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Line must be >= 0");
        
        RuleFor(x => x.Column)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Column must be >= 0");
        
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Code is required");
    }
}
