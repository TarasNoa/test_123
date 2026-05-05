using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Projects.Dtos;
using Libr4.Tasks.Domain.Projects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Projects.Queries;

public sealed record GetProjectsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? Category = null,
    Guid? OwnerId = null
) : IRequest<List<ProjectDto>>;

public sealed class GetProjectsHandler : IRequestHandler<GetProjectsQuery, List<ProjectDto>>
{
    private readonly ITasksDbContext _db;

    public GetProjectsHandler(ITasksDbContext db) => _db = db;

    public async Task<List<ProjectDto>> Handle(GetProjectsQuery query, CancellationToken ct)
    {
        var q = _db.Projects.AsQueryable();

        if (!string.IsNullOrEmpty(query.Status))
        {
            if (Enum.TryParse<ProjectStatus>(query.Status, true, out var status))
                q = q.Where(x => x.Status == status);
        }

        if (!string.IsNullOrEmpty(query.Category))
            q = q.Where(x => x.Category == query.Category);

        if (query.OwnerId.HasValue)
            q = q.Where(x => x.OwnerId == query.OwnerId);

        var projects = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return projects.Select(MapToDto).ToList();
    }

    private static ProjectDto MapToDto(Domain.Projects.Project p) =>
        new(
            p.Id,
            p.Title,
            p.Description,
            p.Category,
            p.Status.ToString(),
            p.OwnerId,
            p.BudgetMin,
            p.BudgetMax,
            p.Budget,
            p.Currency,
            p.Client,
            p.Deadline,
            p.TeamSize,
            p.Progress,
            p.CreatedAt,
            p.UpdatedAt);
}
