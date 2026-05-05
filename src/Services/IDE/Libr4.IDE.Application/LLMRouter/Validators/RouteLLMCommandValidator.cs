using FluentValidation;
using Libr4.IDE.Application.LLMRouter.Commands;

namespace Libr4.IDE.Application.LLMRouter.Validators;

/// <summary>
/// Validator for RouteLLMCommand
/// </summary>
public class RouteLLMCommandValidator : AbstractValidator<RouteLLMCommand>
{
    public RouteLLMCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required");
        
        RuleFor(x => x.Prompt)
            .NotEmpty()
            .WithMessage("Prompt is required")
            .MaximumLength(10000)
            .WithMessage("Prompt must not exceed 10000 characters");
        
        RuleFor(x => x.AvailableModels)
            .NotEmpty()
            .WithMessage("At least one model must be available");
    }
}
