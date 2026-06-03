using Libr4.Shared.Kernel.Results;
using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Domain.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Dashboard.Queries;

public sealed record GetUserPortfolioQuery(Guid UserId) : IRequest<Result<UserPortfolioDto>>;

public sealed record UserPortfolioDto(int TotalTasks, int CompletedTasks, decimal TotalEarnings);

public sealed class GetUserPortfolioHandler : IRequestHandler<GetUserPortfolioQuery, Result<UserPortfolioDto>>
{
    private readonly ITasksDbContext _db;

    public GetUserPortfolioHandler(ITasksDbContext db) => _db = db;

    public async Task<Result<UserPortfolioDto>> Handle(GetUserPortfolioQuery request, CancellationToken ct)
    {
        var assignedTasks = _db.Tasks.AsNoTracking()
            .Where(t => t.AssignedFreelancerId == request.UserId);

        var total = await assignedTasks.CountAsync(ct);
        var completed = await assignedTasks.CountAsync(t => t.Status == Domain.Tasks.TaskStatus.Completed, ct);
        var earnings = await assignedTasks
            .Where(t => t.Status == Domain.Tasks.TaskStatus.Completed && t.Budget > 0)
            .SumAsync(t => t.Budget, ct);

        return Result.Success(new UserPortfolioDto(total, completed, earnings));
    }
}
