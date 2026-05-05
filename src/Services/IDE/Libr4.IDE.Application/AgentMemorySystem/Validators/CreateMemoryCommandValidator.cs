using FluentValidation;
using Libr4.IDE.Application.AgentMemorySystem.Commands;

namespace Libr4.IDE.Application.AgentMemorySystem.Validators;

/// <summary>
/// Validator for CreateMemoryCommand
/// </summary>
public class CreateMemoryCommandValidator : AbstractValidator<CreateMemoryCommand>
{
    public CreateMemoryCommandValidator()
    {
        RuleFor(x => x.AgentId)
            .NotEmpty()
            .WithMessage("Agent ID is required");
    }
}
