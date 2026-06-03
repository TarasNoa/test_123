using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Domain.Posts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Posts.Commands;

public sealed record CreatePostCommand(Guid UserId, string Content, string Title, List<string>? Tags, List<string>? MediaUrls) : IRequest<Result<Guid>>;

public sealed class CreatePostHandler : IRequestHandler<CreatePostCommand, Result<Guid>>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public CreatePostHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreatePostCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<Guid>(Error.Validation("Post.TitleRequired", "Title is required"));

        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<Guid>(Error.Validation("Post.ContentRequired", "Content is required"));

        var post = Post.Create(
            request.UserId,
            request.Title,
            request.Content,
            _clock.UtcNow);

        _db.Posts.Add(post);
        await _db.SaveChangesAsync(ct);

        return Result.Success(post.Id);
    }
}
