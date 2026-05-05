using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Reviews.Queries;

public sealed record GetTaskReviewsQuery(Guid TaskId) : IRequest<IReadOnlyList<ReviewDto>>;

public sealed class GetTaskReviewsHandler : IRequestHandler<GetTaskReviewsQuery, IReadOnlyList<ReviewDto>>
{
    private readonly ITasksDbContext _db;

    public GetTaskReviewsHandler(ITasksDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReviewDto>> Handle(GetTaskReviewsQuery request, CancellationToken ct)
    {
        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.TaskId == request.TaskId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return reviews.Select(r => new ReviewDto(
            r.Id, r.TaskId, r.ReviewerId, "", r.RevieweeId, "",
            r.Rating, r.Comment, r.CreatedAt)).ToList();
    }
}

public sealed record GetUserReviewsQuery(Guid UserId, bool AsReviewee = true) : IRequest<IReadOnlyList<ReviewDto>>;

public sealed class GetUserReviewsHandler : IRequestHandler<GetUserReviewsQuery, IReadOnlyList<ReviewDto>>
{
    private readonly ITasksDbContext _db;

    public GetUserReviewsHandler(ITasksDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReviewDto>> Handle(GetUserReviewsQuery request, CancellationToken ct)
    {
        var query = request.AsReviewee
            ? _db.Reviews.AsNoTracking().Where(r => r.RevieweeId == request.UserId)
            : _db.Reviews.AsNoTracking().Where(r => r.ReviewerId == request.UserId);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return reviews.Select(r => new ReviewDto(
            r.Id, r.TaskId, r.ReviewerId, "", r.RevieweeId, "",
            r.Rating, r.Comment, r.CreatedAt)).ToList();
    }
}
