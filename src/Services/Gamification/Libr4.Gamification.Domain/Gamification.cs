using Libr4.Shared.Kernel.Domain;
using Libr4.Gamification.Domain.Algorithms;

namespace Libr4.Gamification.Domain;

public enum AchievementType
{
    Milestone,
    Skill,
    Social,
    Activity,
    Special
}

public enum AchievementRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public class UserGamification : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public long TotalXP { get; private set; }
    public int Level { get; private set; }
    public long XPToNextLevel { get; private set; }
    public long CurrentLevelXP { get; private set; }
    public int StreakDays { get; private set; }
    public DateTimeOffset LastActivityAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<Achievement> _achievements = new();
    public IReadOnlyCollection<Achievement> Achievements => _achievements.AsReadOnly();

    private readonly List<Badge> _badges = new();
    public IReadOnlyCollection<Badge> Badges => _badges.AsReadOnly();

    private UserGamification() { }

    public static UserGamification Create(Guid userId)
    {
        return new UserGamification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TotalXP = 0,
            Level = 1,
            XPToNextLevel = 1000,
            CurrentLevelXP = 0,
            StreakDays = 0,
            LastActivityAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddXP(long xp)
    {
        TotalXP += xp;
        CurrentLevelXP += xp;
        LastActivityAt = DateTimeOffset.UtcNow;

        // Use F# algorithm to check for level up
        var newLevel = XPSystem.calculateLevelFromXP(TotalXP);
        if (newLevel > Level)
        {
            LevelUpTo(newLevel);
        }
    }

    private void LevelUpTo(int newLevel)
    {
        Level = newLevel;
        XPToNextLevel = XPSystem.calculateXPForLevel(Level + 1);
        CurrentLevelXP = TotalXP - XPSystem.calculateTotalXPToLevel(Level);
    }

    public void IncrementStreak()
    {
        var today = DateTimeOffset.UtcNow.Date;
        var lastActivity = LastActivityAt.Date;

        if (lastActivity == today.AddDays(-1))
        {
            StreakDays++;
        }
        else if (lastActivity < today.AddDays(-1))
        {
            StreakDays = 1;
        }

        LastActivityAt = DateTimeOffset.UtcNow;
    }

    public void ResetStreak()
    {
        StreakDays = 0;
    }

    public void UnlockAchievement(Achievement achievement)
    {
        if (!_achievements.Any(a => a.Id == achievement.Id))
        {
            _achievements.Add(achievement);
            AddXP(achievement.XPReward);
        }
    }

    public void AddBadge(Badge badge)
    {
        if (!_badges.Any(b => b.Id == badge.Id))
        {
            _badges.Add(badge);
        }
    }
}

public class Achievement : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public AchievementType Type { get; private set; }
    public AchievementRarity Rarity { get; private set; }
    public long XPReward { get; private set; }
    public string? IconUrl { get; private set; }
    public Dictionary<string, object> Criteria { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }

    private Achievement() { }

    public static Achievement Create(
        string name,
        string description,
        AchievementType type,
        AchievementRarity rarity,
        long xpReward,
        Dictionary<string, object>? criteria = null,
        string? iconUrl = null)
    {
        return new Achievement
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Type = type,
            Rarity = rarity,
            XPReward = xpReward,
            Criteria = criteria ?? new Dictionary<string, object>(),
            IconUrl = iconUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateCriteria(string key, object value)
    {
        Criteria[key] = value;
    }
}

public class Badge : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public AchievementRarity Rarity { get; private set; }
    public string? IconUrl { get; private set; }
    public DateTimeOffset AwardedAt { get; private set; }
    public bool IsDisplayed { get; private set; }

    private Badge() { }

    public static Badge Create(
        string name,
        string description,
        AchievementRarity rarity,
        string? iconUrl = null)
    {
        return new Badge
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Rarity = rarity,
            IconUrl = iconUrl,
            AwardedAt = DateTimeOffset.UtcNow,
            IsDisplayed = true
        };
    }

    public void Display()
    {
        IsDisplayed = true;
    }

    public void Hide()
    {
        IsDisplayed = false;
    }
}

public class Leaderboard : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public LeaderboardType Type { get; private set; }
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<LeaderboardEntry> _entries = new();
    public IReadOnlyCollection<LeaderboardEntry> Entries => _entries.AsReadOnly();

    private Leaderboard() { }

    public static Leaderboard Create(
        string name,
        string description,
        LeaderboardType type,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd)
    {
        return new Leaderboard
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Type = type,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddEntry(LeaderboardEntry entry)
    {
        _entries.Add(entry);
    }

    public void UpdateEntry(Guid userId, long score)
    {
        var entry = _entries.FirstOrDefault(e => e.UserId == userId);
        if (entry != null)
        {
            entry.UpdateScore(score);
        }
        else
        {
            _entries.Add(LeaderboardEntry.Create(userId, score));
        }

        // Reorder entries
        var sorted = _entries.OrderByDescending(e => e.Score).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].UpdateRank(i + 1);
        }
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

public enum LeaderboardType
{
    Global,
    Weekly,
    Monthly,
    Skill,
    Project
}

public class LeaderboardEntry : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public long Score { get; private set; }
    public int Rank { get; private set; }
    public DateTimeOffset LastUpdated { get; private set; }

    private LeaderboardEntry() { }

    public static LeaderboardEntry Create(Guid userId, long score)
    {
        return new LeaderboardEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Score = score,
            Rank = 0,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    public void UpdateScore(long score)
    {
        Score = score;
        LastUpdated = DateTimeOffset.UtcNow;
    }

    public void UpdateRank(int rank)
    {
        Rank = rank;
    }
}
