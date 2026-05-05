using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Tasks.Commands;

public sealed record CompleteTaskCommand(Guid TaskId, Guid UserId) : IRequest<Result<TaskDto>>;

public sealed class CompleteTaskHandler : IRequestHandler<CompleteTaskCommand, Result<TaskDto>>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public CompleteTaskHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<TaskDto>> Handle(CompleteTaskCommand request, CancellationToken ct)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);
        if (task is null) return Result.Failure<TaskDto>(TasksErrors.TaskNotFound);

        // Only client or assigned freelancer can mark as complete
        if (task.ClientId != request.UserId && task.AssignedFreelancerId != request.UserId)
            return Result.Failure<TaskDto>(TasksErrors.NotTaskOwner);

        try
        {
            task.Complete(_clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<TaskDto>(Error.Validation("tasks.complete_failed", ex.Message));
        }

        await _db.SaveChangesAsync(ct);

        return new TaskDto(
            task.Id, task.Title, task.Description, task.Category.ToString(), task.Status.ToString(),
            task.ClientId, task.AssignedFreelancerId, task.Budget, task.Currency, task.Deadline,
            task.CreatedAt, task.UpdatedAt, task.Applications.Count);
    }
}

public sealed record CancelTaskCommand(Guid TaskId, Guid ClientId) : IRequest<Result<TaskDto>>;

public sealed class CancelTaskHandler : IRequestHandler<CancelTaskCommand, Result<TaskDto>>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public CancelTaskHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<TaskDto>> Handle(CancelTaskCommand request, CancellationToken ct)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);
        if (task is null) return Result.Failure<TaskDto>(TasksErrors.TaskNotFound);
        if (task.ClientId != request.ClientId) return Result.Failure<TaskDto>(TasksErrors.NotTaskOwner);

        try
        {
            task.Cancel(_clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<TaskDto>(Error.Validation("tasks.cancel_failed", ex.Message));
        }

        await _db.SaveChangesAsync(ct);

        return new TaskDto(
            task.Id, task.Title, task.Description, task.Category.ToString(), task.Status.ToString(),
            task.ClientId, task.AssignedFreelancerId, task.Budget, task.Currency, task.Deadline,
            task.CreatedAt, task.UpdatedAt, task.Applications.Count);
    }
}
