using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Portfolio;

public sealed class PortfolioItem : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public PortfolioItemType ItemType { get; private set; }
    public PortfolioItemStatus Status { get; private set; }
    public List<string> Tags { get; private set; } = new();
    public List<string> SkillsUsed { get; private set; } = new();
    public string? Client { get; private set; }
    public string? ProjectUrl { get; private set; }
    public string? GithubUrl { get; private set; }
    public string? LiveUrl { get; private set; }
    public DateTimeOffset? CompletionDate { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public int ViewCount { get; private set; }
    public int LikeCount { get; private set; }
    public int CommentCount { get; private set; }
    public bool Featured { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PortfolioItem() { }

    public static PortfolioItem Create(
        Guid userId,
        string title,
        string description,
        PortfolioItemType itemType,
        List<string>? tags,
        List<string>? skillsUsed,
        string? client,
        string? projectUrl,
        string? githubUrl,
        string? liveUrl,
        DateTimeOffset? completionDate,
        DateTimeOffset now)
    {
        return new PortfolioItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Description = description.Trim(),
            ItemType = itemType,
            Status = PortfolioItemStatus.Draft,
            Tags = tags ?? new(),
            SkillsUsed = skillsUsed ?? new(),
            Client = client?.Trim(),
            ProjectUrl = projectUrl?.Trim(),
            GithubUrl = githubUrl?.Trim(),
            LiveUrl = liveUrl?.Trim(),
            CompletionDate = completionDate,
            Metadata = new(),
            ViewCount = 0,
            LikeCount = 0,
            CommentCount = 0,
            Featured = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string title,
        string description,
        PortfolioItemType itemType,
        List<string>? tags,
        List<string>? skillsUsed,
        string? client,
        string? projectUrl,
        string? githubUrl,
        string? liveUrl,
        DateTimeOffset? completionDate,
        DateTimeOffset now)
    {
        Title = title.Trim();
        Description = description.Trim();
        ItemType = itemType;
        Tags = tags ?? new();
        SkillsUsed = skillsUsed ?? new();
        Client = client?.Trim();
        ProjectUrl = projectUrl?.Trim();
        GithubUrl = githubUrl?.Trim();
        LiveUrl = liveUrl?.Trim();
        CompletionDate = completionDate;
        UpdatedAt = now;
    }

    public void Publish(DateTimeOffset now)
    {
        if (Status == PortfolioItemStatus.Published)
            throw new DomainException("Portfolio item is already published");

        Status = PortfolioItemStatus.Published;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = PortfolioItemStatus.Archived;
        UpdatedAt = now;
    }

    public void MakePrivate(DateTimeOffset now)
    {
        Status = PortfolioItemStatus.Private;
        UpdatedAt = now;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
    }

    public void IncrementLikeCount()
    {
        LikeCount++;
    }

    public void DecrementLikeCount()
    {
        if (LikeCount > 0)
            LikeCount--;
    }

    public void IncrementCommentCount()
    {
        CommentCount++;
    }

    public void DecrementCommentCount()
    {
        if (CommentCount > 0)
            CommentCount--;
    }

    public void SetFeatured(bool featured, DateTimeOffset now)
    {
        Featured = featured;
        UpdatedAt = now;
    }

    public void AddMetadata(string key, object value, DateTimeOffset now)
    {
        Metadata[key] = value;
        UpdatedAt = now;
    }
}

public enum PortfolioItemType
{
    Project = 0,
    Design = 1,
    Code = 2,
    Article = 3,
    Video = 4,
    Audio = 5,
    Document = 6,
    Other = 7
}

public enum PortfolioItemStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
    Private = 3
}
