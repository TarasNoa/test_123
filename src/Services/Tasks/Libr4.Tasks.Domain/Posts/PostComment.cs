using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Posts;

public sealed class PostComment : Entity<Guid>
{
    public Guid PostId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private PostComment() { }

    public static PostComment Create(Guid postId, Guid authorId, string content, DateTimeOffset now)
    {
        return new PostComment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            AuthorId = authorId,
            Content = content.Trim(),
            CreatedAt = now
        };
    }
}
