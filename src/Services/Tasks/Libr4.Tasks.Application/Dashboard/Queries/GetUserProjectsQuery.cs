using Libr4.Shared.Kernel.Results;
using Libr4.Tasks.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Dashboard.Queries;

public sealed record GetUserProjectsQuery(Guid UserId) : IRequest<Result<List<UserProjectDto>>>;

public sealed record UserProjectDto(Guid Id, string Title, string Status, DateTimeOffset CreatedAt);

public sealed class GetUserProjectsHandler : IRequestHandler<GetUserProjectsQuery, Result<List<UserProjectDto>>>
{
    private readonly ITasksDbContext _db;

    public GetUserProjectsHandler(ITasksDbContext db) => _db = db;

    public async Task<Result<List<UserProjectDto>>> Handle(GetUserProjectsQuery request, CancellationToken ct)
    {
        var projects = await _db.Projects
            .AsNoTracking()
            .Where(p => p.OwnerId == request.UserId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new UserProjectDto(p.Id, p.Title, p.Status.ToString(), p.CreatedAt))
            .ToListAsync(ct);

        return Result.Success(projects);
    }
}
