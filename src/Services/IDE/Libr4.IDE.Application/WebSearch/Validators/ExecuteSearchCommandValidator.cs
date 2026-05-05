using FluentValidation;
using Libr4.IDE.Application.WebSearch.Commands;

namespace Libr4.IDE.Application.WebSearch.Validators;

/// <summary>
/// Validator for ExecuteSearchCommand
/// </summary>
public class ExecuteSearchCommandValidator : AbstractValidator<ExecuteSearchCommand>
{
    public ExecuteSearchCommandValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .WithMessage("Query is required")
            .MaximumLength(500)
            .WithMessage("Query must not exceed 500 characters");
        
        RuleFor(x => x.Providers)
            .NotEmpty()
            .WithMessage("At least one provider must be specified");
    }
}
