using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Posts;

public sealed class PostLike : Entity<Guid>
{
    public Guid PostId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PostLike() { }

    public static PostLike Create(Guid postId, Guid userId, DateTimeOffset now)
    {
        return new PostLike
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            UserId = userId,
            CreatedAt = now
        };
    }
}
