using FluentValidation;
using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

using TaskStatus = Libr4.Tasks.Domain.Tasks.TaskStatus;

namespace Libr4.Tasks.Application.Tasks.Commands;

public sealed record ApplyToTaskCommand(Guid TaskId, ApplyToTaskRequest Payload, Guid FreelancerId) : IRequest<Result<ApplicationDto>>;

public sealed class ApplyToTaskValidator : AbstractValidator<ApplyToTaskCommand>
{
    public ApplyToTaskValidator()
    {
        RuleFor(x => x.Payload.Proposal).NotEmpty().MinimumLength(50).MaximumLength(2000);
        RuleFor(x => x.Payload.ProposedBudget).GreaterThan(0);
    }
}

public sealed class ApplyToTaskHandler : IRequestHandler<ApplyToTaskCommand, Result<ApplicationDto>>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public ApplyToTaskHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<ApplicationDto>> Handle(ApplyToTaskCommand request, CancellationToken ct)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);

        if (task is null) return Result.Failure<ApplicationDto>(TasksErrors.TaskNotFound);
        if (task.Status != TaskStatus.Published) return Result.Failure<ApplicationDto>(TasksErrors.TaskNotOpen);
        if (task.ClientId == request.FreelancerId) return Result.Failure<ApplicationDto>(Error.Validation("tasks.self_apply", "Cannot apply to your own task"));

        // Check if application already exists
        var existingApplication = await _db.Applications
            .FirstOrDefaultAsync(a => a.TaskId == request.TaskId && a.FreelancerId == request.FreelancerId && a.Status != Domain.Tasks.ApplicationStatus.Rejected, ct);

        if (existingApplication != null)
        {
            return Result.Failure<ApplicationDto>(Error.Validation("tasks.already_applied", "You have already applied to this task"));
        }

        // Use domain method to create application
        var application = task.Apply(
            request.FreelancerId,
            request.Payload.Proposal,
            request.Payload.ProposedBudget,
            _clock.UtcNow);

        // Add application to DbSet to ensure it's saved properly
        _db.Applications.Add(application);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            // Reload and check if application was added by another request
            var reloadedApplication = await _db.Applications
                .FirstOrDefaultAsync(a => a.TaskId == request.TaskId && a.FreelancerId == request.FreelancerId, ct);

            if (reloadedApplication != null)
            {
                application = reloadedApplication;
            }
            else
            {
                return Result.Failure<ApplicationDto>(Error.Validation("tasks.concurrency_error", "A concurrency error occurred while applying to the task"));
            }
        }

        // Note: Freelancer name would come from a query or cached data in real implementation
        return new ApplicationDto(
            application.Id, application.TaskId, application.FreelancerId,
            "", // Placeholder - would need user service lookup
            application.Proposal, application.ProposedBudget,
            application.Status.ToString(), application.SubmittedAt, application.RespondedAt);
    }
}
