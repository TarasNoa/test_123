using Libr4.Shared.Kernel.Domain;

namespace Libr4.Gamification.Domain.AdvancedGamification;

public enum ChallengeType
{
    Daily,
    Weekly,
    Monthly,
    Seasonal,
    Special
}

public enum ChallengeStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Expired
}

public class Challenge : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ChallengeType Type { get; private set; }
    public ChallengeStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int RewardPoints { get; private set; }
    public string? RewardBadge { get; private set; }
    public int Difficulty { get; private set; }
    public int CurrentProgress { get; private set; }
    public int TargetProgress { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private Challenge() { }

    public static Challenge Create(string title, string description, ChallengeType type, DateTime startDate, DateTime endDate, int rewardPoints, string? rewardBadge, int difficulty, int targetProgress, Guid userId)
    {
        return new Challenge
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Type = type,
            Status = ChallengeStatus.NotStarted,
            StartDate = startDate,
            EndDate = endDate,
            RewardPoints = rewardPoints,
            RewardBadge = rewardBadge,
            Difficulty = difficulty,
            CurrentProgress = 0,
            TargetProgress = targetProgress,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Start()
    {
        if (Status == ChallengeStatus.NotStarted)
        {
            Status = ChallengeStatus.InProgress;
        }
    }

    public void UpdateProgress(int progress)
    {
        CurrentProgress = progress;
        
        if (CurrentProgress >= TargetProgress)
        {
            Complete();
        }
    }

    public void Complete()
    {
        if (Status == ChallengeStatus.InProgress)
        {
            Status = ChallengeStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }
    }

    public void Fail()
    {
        if (Status == ChallengeStatus.InProgress)
        {
            Status = ChallengeStatus.Failed;
            CompletedAt = DateTime.UtcNow;
        }
    }

    public void Expire()
    {
        if (Status == ChallengeStatus.InProgress && DateTime.UtcNow > EndDate)
        {
            Status = ChallengeStatus.Expired;
            CompletedAt = DateTime.UtcNow;
        }
    }

    public float ProgressPercentage
    {
        get
        {
            if (TargetProgress == 0) return 0;
            return (float)CurrentProgress / TargetProgress * 100;
        }
    }
}

public class LeaderboardEntry : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public int Points { get; private set; }
    public int Rank { get; private set; }
    public string Tier { get; private set; } = string.Empty;
    public DateTime LastUpdated { get; private set; }

    private LeaderboardEntry() { }

    public static LeaderboardEntry Create(Guid userId, string username, int points, int rank, string tier)
    {
        return new LeaderboardEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = username,
            Points = points,
            Rank = rank,
            Tier = tier,
            LastUpdated = DateTime.UtcNow
        };
    }

    public void UpdatePoints(int points)
    {
        Points = points;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateRank(int rank)
    {
        Rank = rank;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateTier(string tier)
    {
        Tier = tier;
        LastUpdated = DateTime.UtcNow;
    }
}
