using Libr4.Shared.Kernel.Results;
using Libr4.Tasks.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Dashboard.Queries;

public sealed record GetUserStatsQuery(Guid UserId) : IRequest<Result<UserStatsDto>>;

public sealed record UserStatsDto(int ActiveTasks, int CompletedTasks, decimal Rating);

public sealed class GetUserStatsHandler : IRequestHandler<GetUserStatsQuery, Result<UserStatsDto>>
{
    private readonly ITasksDbContext _db;

    public GetUserStatsHandler(ITasksDbContext db) => _db = db;

    public async Task<Result<UserStatsDto>> Handle(GetUserStatsQuery request, CancellationToken ct)
    {
        var tasks = _db.Tasks.AsNoTracking()
            .Where(t => t.AssignedFreelancerId == request.UserId);

        var active = await tasks.CountAsync(
            t => t.Status == Domain.Tasks.TaskStatus.InProgress || t.Status == Domain.Tasks.TaskStatus.Published,
            ct);

        var completed = await tasks.CountAsync(t => t.Status == Domain.Tasks.TaskStatus.Completed, ct);

        var avgRating = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.RevieweeId == request.UserId)
            .Select(r => (decimal?)r.Rating)
            .AverageAsync(ct) ?? 0m;

        return Result.Success(new UserStatsDto(active, completed, avgRating));
    }
}
