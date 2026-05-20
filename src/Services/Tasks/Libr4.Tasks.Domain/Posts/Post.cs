using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Posts;

public sealed class Post : AggregateRoot<Guid>
{
    public Guid AuthorId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Post() { }

    public static Post Create(Guid authorId, string title, string content, DateTimeOffset now)
    {
        return new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Title = title.Trim(),
            Content = content.Trim(),
            CreatedAt = now
        };
    }
}
