using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Domain.Posts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Posts.Commands;

public sealed record LikePostCommand(Guid PostId, Guid UserId) : IRequest<Result>;

public sealed class LikePostHandler : IRequestHandler<LikePostCommand, Result>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public LikePostHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> Handle(LikePostCommand request, CancellationToken ct)
    {
        var postExists = await _db.Posts.AnyAsync(p => p.Id == request.PostId, ct);
        if (!postExists)
            return Result.Failure(Error.NotFound("Post.NotFound", "Post not found"));

        var alreadyLiked = await _db.PostLikes
            .AnyAsync(l => l.PostId == request.PostId && l.UserId == request.UserId, ct);

        if (alreadyLiked)
            return Result.Success();

        _db.PostLikes.Add(PostLike.Create(request.PostId, request.UserId, _clock.UtcNow));
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
