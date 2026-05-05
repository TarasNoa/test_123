using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Skills;

public sealed class SkillTest : AggregateRoot<Guid>
{
    public string SkillName { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public SkillTestKind Kind { get; private set; }
    public int DurationMinutes { get; private set; }
    public int PassingScore { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }

    private SkillTest() { }

    public static SkillTest Create(string skillName, string title, string? description,
        SkillTestKind kind, int duration, int passingScore, DateTimeOffset now)
    {
        return new SkillTest
        {
            Id = Guid.NewGuid(),
            SkillName = skillName,
            Title = title,
            Description = description,
            Kind = kind,
            DurationMinutes = duration,
            PassingScore = passingScore,
            CreatedAt = now
        };
    }

    public void Deactivate() => IsActive = false;
}

public sealed class SkillTestAttempt : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid SkillTestId { get; private set; }
    public SkillAttemptStatus Status { get; private set; }
    public int? Score { get; private set; }
    public bool? Passed { get; private set; }
    public string? AnswersJson { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private SkillTestAttempt() { }

    public static SkillTestAttempt Start(Guid userId, Guid skillTestId, DateTimeOffset now)
    {
        return new SkillTestAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SkillTestId = skillTestId,
            Status = SkillAttemptStatus.InProgress,
            StartedAt = now
        };
    }

    public void Submit(int score, int passingScore, string? answersJson, DateTimeOffset now)
    {
        Score = score;
        Passed = score >= passingScore;
        AnswersJson = answersJson;
        Status = SkillAttemptStatus.Completed;
        CompletedAt = now;
    }

    public void Abandon(DateTimeOffset now)
    {
        Status = SkillAttemptStatus.Abandoned;
        CompletedAt = now;
    }
}

public sealed class SkillCertificate : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string SkillName { get; private set; } = "";
    public Guid AttemptId { get; private set; }
    public int Score { get; private set; }
    public string CertificateNumber { get; private set; } = "";
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private SkillCertificate() { }

    public static SkillCertificate Issue(Guid userId, string skillName, Guid attemptId, int score, DateTimeOffset now, TimeSpan? validity = null)
    {
        return new SkillCertificate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SkillName = skillName,
            AttemptId = attemptId,
            Score = score,
            CertificateNumber = $"L4-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            IssuedAt = now,
            ExpiresAt = now.Add(validity ?? TimeSpan.FromDays(730))
        };
    }
}

public enum SkillTestKind { Quiz = 0, Coding = 1, PortfolioReview = 2, LiveInterview = 3 }
public enum SkillAttemptStatus { InProgress = 0, Completed = 1, Abandoned = 2, Disqualified = 3 }
