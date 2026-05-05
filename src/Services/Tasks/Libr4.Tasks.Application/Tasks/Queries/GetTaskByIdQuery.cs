using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Domain;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

using TaskStatus = Libr4.Tasks.Domain.Tasks.TaskStatus;

namespace Libr4.Tasks.Application.Tasks.Queries;

public sealed record GetTaskByIdQuery(Guid TaskId, Guid? CurrentUserId = null) : IRequest<Result<TaskDetailDto>>;

public sealed class GetTaskByIdHandler : IRequestHandler<GetTaskByIdQuery, Result<TaskDetailDto>>
{
    private readonly ITasksDbContext _db;

    public GetTaskByIdHandler(ITasksDbContext db) => _db = db;

    public async Task<Result<TaskDetailDto>> Handle(GetTaskByIdQuery request, CancellationToken ct)
    {
        var task = await _db.Tasks
            .AsNoTracking()
            .Include(t => t.Applications)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);

        if (task is null) return Result.Failure<TaskDetailDto>(TasksErrors.TaskNotFound);

        // Hide draft tasks from non-owners
        if (task.Status == TaskStatus.Draft && task.ClientId != request.CurrentUserId)
            return Result.Failure<TaskDetailDto>(TasksErrors.TaskNotFound);

        var applications = task.Applications.Select(a => new ApplicationDto(
            a.Id, a.TaskId, a.FreelancerId, "", a.Proposal, a.ProposedBudget,
            a.Status.ToString(), a.SubmittedAt, a.RespondedAt)).ToList();

        return new TaskDetailDto(
            task.Id, task.Title, task.Description, task.Category.ToString(), task.Status.ToString(),
            task.ClientId, "", task.AssignedFreelancerId, "",
            task.Budget, task.Currency, task.Deadline,
            task.CreatedAt, task.UpdatedAt, task.PublishedAt, task.CompletedAt,
            applications);
    }
}
