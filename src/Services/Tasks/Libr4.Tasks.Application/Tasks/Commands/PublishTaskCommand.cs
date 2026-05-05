using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Tasks.Commands;

public sealed record PublishTaskCommand(Guid TaskId, Guid ClientId) : IRequest<Result<TaskDto>>;

public sealed class PublishTaskHandler : IRequestHandler<PublishTaskCommand, Result<TaskDto>>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public PublishTaskHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<TaskDto>> Handle(PublishTaskCommand request, CancellationToken ct)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);
        if (task is null) return Result.Failure<TaskDto>(TasksErrors.TaskNotFound);
        if (task.ClientId != request.ClientId) return Result.Failure<TaskDto>(TasksErrors.NotTaskOwner);

        try
        {
            task.Publish(_clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<TaskDto>(Error.Validation("tasks.publish_failed", ex.Message));
        }

        await _db.SaveChangesAsync(ct);

        return new TaskDto(
            task.Id, task.Title, task.Description, task.Category.ToString(), task.Status.ToString(),
            task.ClientId, task.AssignedFreelancerId, task.Budget, task.Currency, task.Deadline,
            task.CreatedAt, task.UpdatedAt, task.Applications.Count);
    }
}
