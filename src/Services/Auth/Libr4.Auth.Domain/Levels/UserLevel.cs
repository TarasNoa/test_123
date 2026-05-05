using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Levels;

public sealed class UserLevel : AggregateRoot<Guid>
{
    private readonly List<XpEvent> _events = new();

    public Guid UserId { get; private set; }
    public int Xp { get; private set; }
    public int Level { get; private set; } = 1;
    public decimal ReputationScore { get; private set; }
    public int TasksCompleted { get; private set; }
    public int FiveStarReviews { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<XpEvent> Events => _events.AsReadOnly();

    private UserLevel() { }

    public static UserLevel Create(Guid userId, DateTimeOffset now)
    {
        return new UserLevel { Id = Guid.NewGuid(), UserId = userId, CreatedAt = now, UpdatedAt = now };
    }

    public void GrantXp(int amount, XpReason reason, string? referenceId, DateTimeOffset now)
    {
        if (amount <= 0) return;
        Xp += amount;
        _events.Add(new XpEvent(Id, amount, reason, referenceId, now));
        var newLevel = ComputeLevel(Xp);
        if (newLevel > Level) Level = newLevel;
        UpdatedAt = now;
    }

    public void RegisterTaskCompletion(decimal? rating, DateTimeOffset now)
    {
        TasksCompleted++;
        if (rating.HasValue && rating.Value >= 5) FiveStarReviews++;
        // Reputation: weighted avg toward last 50 tasks
        if (rating.HasValue)
        {
            var weight = Math.Min(TasksCompleted, 50);
            ReputationScore = ((ReputationScore * (weight - 1)) + rating.Value) / weight;
        }
        GrantXp(50, XpReason.TaskCompleted, null, now);
    }

    public static int ComputeLevel(int xp)
    {
        // Simple logarithmic curve: L1=0, L2=100, L3=300, L4=600, L5=1000, ...
        if (xp <= 0) return 1;
        var lvl = 1 + (int)Math.Floor(Math.Sqrt(xp / 50.0));
        return Math.Max(1, Math.Min(100, lvl));
    }

    public int XpToNextLevel()
    {
        var next = Level + 1;
        var requiredXp = (int)Math.Pow(next - 1, 2) * 50;
        return Math.Max(0, requiredXp - Xp);
    }
}

public sealed class XpEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserLevelId { get; private set; }
    public int Amount { get; private set; }
    public XpReason Reason { get; private set; }
    public string? ReferenceId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private XpEvent() { }
    internal XpEvent(Guid levelId, int amount, XpReason reason, string? refId, DateTimeOffset now)
    {
        UserLevelId = levelId;
        Amount = amount;
        Reason = reason;
        ReferenceId = refId;
        OccurredAt = now;
    }
}

public enum XpReason
{
    Registration = 0,
    EmailConfirmed = 1,
    ProfileCompleted = 2,
    KycVerified = 3,
    FirstTaskCreated = 4,
    TaskCompleted = 5,
    ReviewReceived = 6,
    SkillVerified = 7,
    OnboardingCompleted = 8,
    Achievement = 9,
    Referral = 10,
    DailyLogin = 11,
    Manual = 99
}
