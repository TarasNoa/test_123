using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TeamsPortfolio;

public sealed class Review : AggregateRoot<Guid>
{
    public Guid ReviewerId { get; private set; }
    public Guid TargetId { get; private set; }
    public ReviewTargetType TargetType { get; private set; }
    public Guid? TaskId { get; private set; }
    public float OverallScore { get; private set; }
    public Dictionary<string, float> CriteriaScores { get; private set; } = new();
    public string? ReviewText { get; private set; }
    public List<string> Strengths { get; private set; } = new();
    public List<string> Improvements { get; private set; } = new();
    public bool? WouldHireAgain { get; private set; }
    public bool? WouldRecommend { get; private set; }
    public bool IsPublic { get; private set; } = true;
    public bool IsVerified { get; private set; }
    public bool IsFeatured { get; private set; }
    public string? ResponseText { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public Guid? RespondedBy { get; private set; }
    public int HelpfulVotes { get; private set; }
    public int ReportCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Review() { }

    public static Review Create(
        Guid reviewerId,
        Guid targetId,
        ReviewTargetType targetType,
        float overallScore,
        string? reviewText,
        Guid? taskId,
        DateTimeOffset now)
    {
        return new Review
        {
            Id = Guid.NewGuid(),
            ReviewerId = reviewerId,
            TargetId = targetId,
            TargetType = targetType,
            TaskId = taskId,
            OverallScore = overallScore,
            ReviewText = reviewText?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetCriteria(Dictionary<string, float> scores, DateTimeOffset now)
    {
        CriteriaScores = scores ?? new();
        UpdatedAt = now;
    }

    public void AddStrengths(List<string> strengths, DateTimeOffset now)
    {
        Strengths = strengths ?? new();
        UpdatedAt = now;
    }

    public void AddImprovements(List<string> improvements, DateTimeOffset now)
    {
        Improvements = improvements ?? new();
        UpdatedAt = now;
    }

    public void SetRecommendation(bool wouldHireAgain, bool wouldRecommend, DateTimeOffset now)
    {
        WouldHireAgain = wouldHireAgain;
        WouldRecommend = wouldRecommend;
        UpdatedAt = now;
    }

    public void Respond(string responseText, Guid respondedBy, DateTimeOffset now)
    {
        ResponseText = responseText.Trim();
        RespondedBy = respondedBy;
        RespondedAt = now;
        UpdatedAt = now;
    }

    public void Verify(DateTimeOffset now)
    {
        IsVerified = true;
        UpdatedAt = now;
    }

    public void Feature(DateTimeOffset now)
    {
        IsFeatured = true;
        UpdatedAt = now;
    }

    public void AddHelpfulVote()
    {
        HelpfulVotes++;
    }

    public void AddReport()
    {
        ReportCount++;
    }

    public bool IsPositive => OverallScore >= 4.0f;
}

public sealed class RateHistory : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public RateType RateType { get; private set; }
    public float RateAmount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public List<string> Skills { get; private set; } = new();
    public string? ProjectType { get; private set; }
    public string? ExperienceLevel { get; private set; }
    public DateTimeOffset EffectiveDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    public bool IsCurrent { get; private set; } = true;
    public string? ReasonForChange { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RateHistory() { }

    public static RateHistory Create(
        Guid userId,
        RateType rateType,
        float rateAmount,
        string currency,
        DateTimeOffset effectiveDate,
        DateTimeOffset now)
    {
        return new RateHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RateType = rateType,
            RateAmount = rateAmount,
            Currency = currency,
            EffectiveDate = effectiveDate,
            CreatedAt = now
        };
    }

    public void End(DateTimeOffset endDate)
    {
        EndDate = endDate;
        IsCurrent = false;
    }
}

public enum ReviewTargetType
{
    User = 0,
    Team = 1
}

public enum RateType
{
    Hourly = 0,
    Fixed = 1,
    Project = 2
}
