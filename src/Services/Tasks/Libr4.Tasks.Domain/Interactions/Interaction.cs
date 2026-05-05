using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Interactions;

public sealed class Like : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid TargetId { get; private set; } // Task, Project, Review, etc.
    public InteractionTargetType TargetType { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Like() { }

    public static Like Create(Guid userId, Guid targetId, InteractionTargetType targetType, DateTimeOffset now)
    {
        return new Like
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetId = targetId,
            TargetType = targetType,
            CreatedAt = now
        };
    }
}

public sealed class Bookmark : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid TargetId { get; private set; }
    public InteractionTargetType TargetType { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Bookmark() { }

    public static Bookmark Create(Guid userId, Guid targetId, InteractionTargetType targetType, string? notes, DateTimeOffset now)
    {
        return new Bookmark
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetId = targetId,
            TargetType = targetType,
            Notes = notes?.Trim(),
            CreatedAt = now
        };
    }

    public void UpdateNotes(string? notes, DateTimeOffset now)
    {
        Notes = notes?.Trim();
    }
}

public sealed class Follow : AggregateRoot<Guid>
{
    public Guid FollowerId { get; private set; }
    public Guid FollowingId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Follow() { }

    public static Follow Create(Guid followerId, Guid followingId, DateTimeOffset now)
    {
        if (followerId == followingId)
            throw new DomainException("Cannot follow yourself");

        return new Follow
        {
            Id = Guid.NewGuid(),
            FollowerId = followerId,
            FollowingId = followingId,
            CreatedAt = now
        };
    }
}

public sealed class View : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid TargetId { get; private set; }
    public InteractionTargetType TargetType { get; private set; }
    public DateTimeOffset ViewedAt { get; private set; }

    private View() { }

    public static View Create(Guid userId, Guid targetId, InteractionTargetType targetType, DateTimeOffset now)
    {
        return new View
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetId = targetId,
            TargetType = targetType,
            ViewedAt = now
        };
    }
}

public enum InteractionTargetType
{
    Task = 0,
    Project = 1,
    Review = 2,
    Application = 3,
    Portfolio = 4,
    User = 5
}
