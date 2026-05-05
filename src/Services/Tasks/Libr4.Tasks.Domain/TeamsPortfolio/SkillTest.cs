using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TeamsPortfolio;

public sealed class SkillTest : AggregateRoot<Guid>
{
    public string Name { get; private set; } = "";
    public string? Description { get; private set; }
    public string Category { get; private set; } = "";
    public TestDifficulty Difficulty { get; private set; }
    public int DurationMinutes { get; private set; } = 30;
    public int QuestionCount { get; private set; }
    public float PassingScore { get; private set; } = 70.0f;
    public int MaxAttempts { get; private set; } = 3;
    public Dictionary<string, object> Questions { get; private set; } = new();
    public string? Instructions { get; private set; }
    public Dictionary<string, object> Resources { get; private set; } = new();
    public bool IsActive { get; private set; } = true;
    public bool IsPublic { get; private set; } = true;
    public bool RequiresProctoring { get; private set; }
    public int AttemptsCount { get; private set; }
    public float? PassRate { get; private set; }
    public float? AverageScore { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<SkillTestResult> _results = new();

    public IReadOnlyCollection<SkillTestResult> Results => _results.AsReadOnly();

    private SkillTest() { }

    public static SkillTest Create(
        string name,
        string category,
        int questionCount,
        TestDifficulty difficulty,
        DateTimeOffset now)
    {
        return new SkillTest
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Category = category.Trim(),
            QuestionCount = questionCount,
            Difficulty = difficulty,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDetails(string name, string? description, int duration, float passingScore, int maxAttempts, DateTimeOffset now)
    {
        Name = name.Trim();
        Description = description?.Trim();
        DurationMinutes = duration;
        PassingScore = passingScore;
        MaxAttempts = maxAttempts;
        UpdatedAt = now;
    }

    public void SetQuestions(Dictionary<string, object> questions, DateTimeOffset now)
    {
        Questions = questions ?? new();
        UpdatedAt = now;
    }

    public void SetInstructions(string? instructions, DateTimeOffset now)
    {
        Instructions = instructions?.Trim();
        UpdatedAt = now;
    }

    public void SetResources(Dictionary<string, object> resources, DateTimeOffset now)
    {
        Resources = resources ?? new();
        UpdatedAt = now;
    }

    public void Publish(DateTimeOffset now)
    {
        IsActive = true;
        IsPublic = true;
        UpdatedAt = now;
    }

    public void Unpublish(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    public void AddResult(SkillTestResult result, DateTimeOffset now)
    {
        _results.Add(result);
        AttemptsCount++;
        UpdatedAt = now;
    }

    public void UpdateStats(float passRate, float averageScore, DateTimeOffset now)
    {
        PassRate = passRate;
        AverageScore = averageScore;
        UpdatedAt = now;
    }
}

public sealed class SkillTestResult : AggregateRoot<Guid>
{
    public Guid TestId { get; private set; }
    public Guid UserId { get; private set; }
    public float Score { get; private set; }
    public bool Passed { get; private set; }
    public Dictionary<string, object> Answers { get; private set; } = new();
    public int TimeTakenSeconds { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset CompletedAt { get; private set; }
    public int AttemptNumber { get; private set; } = 1;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public bool IsVerified { get; private set; }
    public string? VerificationMethod { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SkillTestResult() { }

    public static SkillTestResult Create(
        Guid testId,
        Guid userId,
        float score,
        int timeTakenSeconds,
        float passingScore,
        DateTimeOffset completedAt)
    {
        return new SkillTestResult
        {
            Id = Guid.NewGuid(),
            TestId = testId,
            UserId = userId,
            Score = score,
            Passed = score >= passingScore,
            TimeTakenSeconds = timeTakenSeconds,
            CompletedAt = completedAt,
            CreatedAt = completedAt
        };
    }

    public void SetAnswers(Dictionary<string, object> answers)
    {
        Answers = answers ?? new();
    }

    public void SetTiming(DateTimeOffset? startedAt, int timeTaken)
    {
        StartedAt = startedAt;
        TimeTakenSeconds = timeTaken;
    }

    public void SetEnvironment(string? ipAddress, string? userAgent)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public void Verify(string verificationMethod)
    {
        IsVerified = true;
        VerificationMethod = verificationMethod;
    }
}

public enum TestDifficulty
{
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2,
    Expert = 3
}
