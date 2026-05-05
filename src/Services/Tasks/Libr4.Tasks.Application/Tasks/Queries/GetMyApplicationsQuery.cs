using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Tasks.Queries;

public sealed record GetMyApplicationsQuery(Guid FreelancerId, string? Status = null) : IRequest<IReadOnlyList<ApplicationDto>>;

public sealed class GetMyApplicationsHandler : IRequestHandler<GetMyApplicationsQuery, IReadOnlyList<ApplicationDto>>
{
    private readonly ITasksDbContext _db;

    public GetMyApplicationsHandler(ITasksDbContext db) => _db = db;

    public async Task<IReadOnlyList<ApplicationDto>> Handle(GetMyApplicationsQuery request, CancellationToken ct)
    {
        var query = _db.Applications
            .AsNoTracking()
            .Where(a => a.FreelancerId == request.FreelancerId);

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<Domain.Tasks.ApplicationStatus>(request.Status, true, out var status))
            query = query.Where(a => a.Status == status);

        var applications = await query
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(ct);

        return applications.Select(a => new ApplicationDto(
            a.Id, a.TaskId, a.FreelancerId, "", a.Proposal, a.ProposedBudget,
            a.Status.ToString(), a.SubmittedAt, a.RespondedAt)).ToList();
    }
}

public sealed record GetTaskApplicationsQuery(Guid TaskId, Guid ClientId) : IRequest<IReadOnlyList<ApplicationDto>>;

public sealed class GetTaskApplicationsHandler : IRequestHandler<GetTaskApplicationsQuery, IReadOnlyList<ApplicationDto>>
{
    private readonly ITasksDbContext _db;

    public GetTaskApplicationsHandler(ITasksDbContext db) => _db = db;

    public async Task<IReadOnlyList<ApplicationDto>> Handle(GetTaskApplicationsQuery request, CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync(new object[] { request.TaskId }, ct);
        if (task is null || task.ClientId != request.ClientId)
            return Array.Empty<ApplicationDto>();

        var applications = await _db.Applications
            .AsNoTracking()
            .Where(a => a.TaskId == request.TaskId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(ct);

        return applications.Select(a => new ApplicationDto(
            a.Id, a.TaskId, a.FreelancerId, "", a.Proposal, a.ProposedBudget,
            a.Status.ToString(), a.SubmittedAt, a.RespondedAt)).ToList();
    }
}
