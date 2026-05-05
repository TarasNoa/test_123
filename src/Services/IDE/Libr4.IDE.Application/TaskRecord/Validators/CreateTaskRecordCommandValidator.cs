using FluentValidation;
using Libr4.IDE.Application.TaskRecord.Commands;

namespace Libr4.IDE.Application.TaskRecord.Validators;

/// <summary>
/// Validator for CreateTaskRecordCommand
/// </summary>
public class CreateTaskRecordCommandValidator : AbstractValidator<CreateTaskRecordCommand>
{
    public CreateTaskRecordCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required");
    }
}
