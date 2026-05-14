using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Domain.Tasks;
using TaskStatus = Libr4.Tasks.Domain.Tasks.TaskStatus;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Tasks.Queries;

public sealed record GetTasksQuery(
    string? Status = null,
    string? Category = null,
    Guid? ClientId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<TaskDto>>;

public sealed class GetTasksHandler : IRequestHandler<GetTasksQuery, PagedResult<TaskDto>>
{
    private readonly ITasksDbContext _db;

    public GetTasksHandler(ITasksDbContext db) => _db = db;

    public async Task<PagedResult<TaskDto>> Handle(GetTasksQuery request, CancellationToken ct)
    {
        var query = _db.Tasks.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<TaskStatus>(request.Status, true, out var status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrEmpty(request.Category) && Enum.TryParse<TaskCategory>(request.Category, true, out var category))
            query = query.Where(t => t.Category == category);

        if (request.ClientId.HasValue)
            query = query.Where(t => t.ClientId == request.ClientId.Value);

        // Only show published tasks for public listing (unless filtering by client)
        if (!request.ClientId.HasValue)
            query = query.Where(t => t.Status == TaskStatus.Published);

        var total = await query.CountAsync(ct);

        var tasks = await query
            .OrderByDescending(t => t.PublishedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = tasks.Select(t => new TaskDto(
            t.Id, t.Title, t.Description, t.Category.ToString(), t.Status.ToString(),
            t.ClientId, t.AssignedFreelancerId, t.Budget, t.Currency, t.Deadline,
            t.CreatedAt, t.UpdatedAt, t.Applications.Count)).ToList();

        return new PagedResult<TaskDto>(
            items, total, request.Page, request.PageSize,
            (int)Math.Ceiling(total / (double)request.PageSize));
    }
}
