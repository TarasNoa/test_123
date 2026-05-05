using FluentValidation;
using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Domain;
using Libr4.Tasks.Domain.Tasks;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Tasks.Commands;

public sealed record UpdateTaskCommand(Guid TaskId, UpdateTaskRequest Payload, Guid ClientId) : IRequest<Result<TaskDto>>;

public sealed class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.Payload.Title).NotEmpty().MinimumLength(10).MaximumLength(200);
        RuleFor(x => x.Payload.Description).NotEmpty().MinimumLength(50).MaximumLength(5000);
        RuleFor(x => x.Payload.Budget).GreaterThan(0);
        RuleFor(x => x.Payload.Currency).NotEmpty().Length(3);
    }
}

public sealed class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand, Result<TaskDto>>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public UpdateTaskHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<TaskDto>> Handle(UpdateTaskCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<TaskCategory>(request.Payload.Category, true, out var category))
            return Result.Failure<TaskDto>(TasksErrors.InvalidStatusTransition);

        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);
        if (task is null) return Result.Failure<TaskDto>(TasksErrors.TaskNotFound);
        if (task.ClientId != request.ClientId) return Result.Failure<TaskDto>(TasksErrors.NotTaskOwner);

        try
        {
            task.Update(
                request.Payload.Title,
                request.Payload.Description,
                category,
                request.Payload.Budget,
                request.Payload.Currency,
                request.Payload.Deadline,
                _clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<TaskDto>(Error.Validation("tasks.update_failed", ex.Message));
        }

        await _db.SaveChangesAsync(ct);

        return new TaskDto(
            task.Id, task.Title, task.Description, task.Category.ToString(), task.Status.ToString(),
            task.ClientId, task.AssignedFreelancerId, task.Budget, task.Currency, task.Deadline,
            task.CreatedAt, task.UpdatedAt, task.Applications.Count);
    }
}
