using Libr4.Shared.Kernel.Results;
using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Posts.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Posts.Queries;

public sealed record GetFeedQuery([FromQuery] int Page = 1, [FromQuery] int PageSize = 20) : IRequest<Result<List<PostDto>>>;

public sealed class GetFeedHandler : IRequestHandler<GetFeedQuery, Result<List<PostDto>>>
{
    private readonly ITasksDbContext _db;

    public GetFeedHandler(ITasksDbContext db) => _db = db;

    public async Task<Result<List<PostDto>>> Handle(GetFeedQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var posts = await _db.Posts
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(p => new PostDto(p.Id, p.AuthorId, p.Title, p.Content, p.CreatedAt))
            .ToListAsync(ct);

        return Result.Success(posts);
    }
}
