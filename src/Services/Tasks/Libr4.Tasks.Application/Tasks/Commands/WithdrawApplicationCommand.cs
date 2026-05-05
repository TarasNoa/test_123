using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Tasks.Commands;

public sealed record WithdrawApplicationCommand(Guid ApplicationId, Guid FreelancerId) : IRequest<Result>;

public sealed class WithdrawApplicationHandler : IRequestHandler<WithdrawApplicationCommand, Result>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public WithdrawApplicationHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> Handle(WithdrawApplicationCommand request, CancellationToken ct)
    {
        var application = await _db.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        if (application is null) return Result.Failure(TasksErrors.ApplicationNotFound);
        if (application.FreelancerId != request.FreelancerId)
            return Result.Failure(TasksErrors.NotApplicationOwner);

        try
        {
            application.Withdraw(_clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Validation("tasks.withdraw_failed", ex.Message));
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
