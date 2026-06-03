using Libr4.Shared.Kernel.Results;
using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Posts.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Posts.Queries;

public sealed record GetMyPostsQuery(Guid UserId) : IRequest<Result<List<PostDto>>>;

public sealed class GetMyPostsHandler : IRequestHandler<GetMyPostsQuery, Result<List<PostDto>>>
{
    private readonly ITasksDbContext _db;

    public GetMyPostsHandler(ITasksDbContext db) => _db = db;

    public async Task<Result<List<PostDto>>> Handle(GetMyPostsQuery request, CancellationToken ct)
    {
        var posts = await _db.Posts
            .AsNoTracking()
            .Where(p => p.AuthorId == request.UserId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PostDto(p.Id, p.AuthorId, p.Title, p.Content, p.CreatedAt))
            .ToListAsync(ct);

        return Result.Success(posts);
    }
}
