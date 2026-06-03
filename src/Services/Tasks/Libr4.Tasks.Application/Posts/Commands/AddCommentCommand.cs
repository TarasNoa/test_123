using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Domain.Posts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Posts.Commands;

public sealed record AddCommentCommand(Guid PostId, Guid UserId, string Content) : IRequest<Result<Guid>>;

public sealed class AddCommentHandler : IRequestHandler<AddCommentCommand, Result<Guid>>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public AddCommentHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(AddCommentCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<Guid>(Error.Validation("Comment.ContentRequired", "Comment content is required"));

        var postExists = await _db.Posts.AnyAsync(p => p.Id == request.PostId, ct);
        if (!postExists)
            return Result.Failure<Guid>(Error.NotFound("Post.NotFound", "Post not found"));

        var comment = PostComment.Create(request.PostId, request.UserId, request.Content, _clock.UtcNow);
        _db.PostComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        return Result.Success(comment.Id);
    }
}
