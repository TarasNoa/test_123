using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Domain;
using Libr4.Shared.Contracts.IntegrationEvents.Tasks;
using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Tasks.Commands;

public sealed record AcceptApplicationCommand(Guid TaskId, Guid ApplicationId, Guid ClientId) : IRequest<Result<TaskDto>>;

public sealed class AcceptApplicationHandler : IRequestHandler<AcceptApplicationCommand, Result<TaskDto>>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;
    private readonly IEventBus _eventBus;

    public AcceptApplicationHandler(ITasksDbContext db, IClock clock, IEventBus eventBus)
    {
        _db = db;
        _clock = clock;
        _eventBus = eventBus;
    }

    public async Task<Result<TaskDto>> Handle(AcceptApplicationCommand request, CancellationToken ct)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);

        if (task is null) return Result.Failure<TaskDto>(TasksErrors.TaskNotFound);
        if (task.ClientId != request.ClientId) return Result.Failure<TaskDto>(TasksErrors.NotTaskOwner);

        var application = await _db.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.TaskId == request.TaskId, ct);

        if (application is null)
            return Result.Failure<TaskDto>(Error.Validation("tasks.application_not_found", "Application not found"));

        if (application.Status != Domain.Tasks.ApplicationStatus.Pending)
            return Result.Failure<TaskDto>(Error.Validation("tasks.application_not_pending", $"Application is not pending, current status: {application.Status}"));

        try
        {
            task.AcceptApplicationById(request.ApplicationId, application.FreelancerId, _clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<TaskDto>(Error.Validation("tasks.accept_failed", ex.Message));
        }

        application.Accept(_clock.UtcNow);

        // Reject other pending applications
        var otherApplications = await _db.Applications
            .Where(a => a.TaskId == request.TaskId && a.Id != request.ApplicationId && a.Status == Domain.Tasks.ApplicationStatus.Pending)
            .ToListAsync(ct);

        foreach (var other in otherApplications)
        {
            other.Reject(_clock.UtcNow);
        }

        await _db.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(new ApplicationAcceptedIntegrationEvent(
            TaskId: task.Id,
            ApplicationId: request.ApplicationId,
            ClientId: task.ClientId,
            FreelancerId: application.FreelancerId,
            Amount: application.ProposedBudget,
            Currency: task.Currency,
            OccurredOn: _clock.UtcNow), ct);

        var applicationCount = await _db.Applications
            .CountAsync(a => a.TaskId == request.TaskId, ct);

        return new TaskDto(
            task.Id, task.Title, task.Description, task.Category.ToString(), task.Status.ToString(),
            task.ClientId, task.AssignedFreelancerId, task.Budget, task.Currency, task.Deadline,
            task.CreatedAt, task.UpdatedAt, applicationCount);
    }
}
