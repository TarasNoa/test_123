using Libr4.Shared.Kernel.Domain;

namespace Libr4.Community.Domain.Forums;

public enum ForumCategory
{
    General,
    Technical,
    Business,
    OffTopic,
    Support
}

public enum PostStatus
{
    Draft,
    Published,
    Archived,
    Locked,
    Deleted
}

public class Forum : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ForumCategory Category { get; private set; }
    public bool IsActive { get; private set; }
    public int TopicCount { get; private set; }
    public int PostCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<Topic> _topics = new();
    public IReadOnlyCollection<Topic> Topics => _topics.AsReadOnly();

    private Forum() { }

    public static Forum Create(string name, string description, ForumCategory category)
    {
        return new Forum
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Category = category,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddTopic(Topic topic)
    {
        _topics.Add(topic);
        TopicCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void IncrementPostCount()
    {
        PostCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public class Topic : Entity<Guid>
{
    public Guid ForumId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public PostStatus Status { get; private set; }
    public bool IsPinned { get; private set; }
    public bool IsLocked { get; private set; }
    public int ViewCount { get; private set; }
    public int ReplyCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastReplyAt { get; private set; }

    // ML moderation
    public bool? IsApproved { get; private set; }
    public float? ModerationScore { get; private set; }

    private readonly List<Post> _posts = new();
    public IReadOnlyCollection<Post> Posts => _posts.AsReadOnly();

    private Topic() { }

    public static Topic Create(Guid forumId, Guid authorId, string title, string content)
    {
        return new Topic
        {
            Id = Guid.NewGuid(),
            ForumId = forumId,
            AuthorId = authorId,
            Title = title,
            Content = content,
            Status = PostStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddPost(Post post)
    {
        _posts.Add(post);
        ReplyCount++;
        LastReplyAt = DateTimeOffset.UtcNow;
    }

    public void Pin()
    {
        IsPinned = true;
    }

    public void Unpin()
    {
        IsPinned = false;
    }

    public void Lock()
    {
        IsLocked = true;
    }

    public void Unlock()
    {
        IsLocked = false;
    }

    public void RecordView()
    {
        ViewCount++;
    }

    public void SetModerationResult(bool isApproved, float score)
    {
        IsApproved = isApproved;
        ModerationScore = score;
    }

    public void Archive()
    {
        Status = PostStatus.Archived;
    }
}

public class Post : Entity<Guid>
{
    public Guid TopicId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public PostStatus Status { get; private set; }
    public int LikeCount { get; private set; }
    public int DislikeCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? EditedAt { get; private set; }

    // ML moderation
    public bool? IsApproved { get; private set; }
    public float? ModerationScore { get; private set; }

    private Post() { }

    public static Post Create(Guid topicId, Guid authorId, string content)
    {
        return new Post
        {
            Id = Guid.NewGuid(),
            TopicId = topicId,
            AuthorId = authorId,
            Content = content,
            Status = PostStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Edit(string newContent)
    {
        Content = newContent;
        EditedAt = DateTimeOffset.UtcNow;
    }

    public void Like()
    {
        LikeCount++;
    }

    public void Unlike()
    {
        if (LikeCount > 0)
            LikeCount--;
    }

    public void Dislike()
    {
        DislikeCount++;
    }

    public void SetModerationResult(bool isApproved, float score)
    {
        IsApproved = isApproved;
        ModerationScore = score;
    }

    public void Delete()
    {
        Status = PostStatus.Deleted;
    }
}
