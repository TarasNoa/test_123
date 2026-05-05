using Libr4.Shared.Kernel.Domain;

namespace Libr4.Education.Domain.Levels;

public enum LevelTier
{
    Bronze,
    Silver,
    Gold,
    Platinum,
    Diamond,
    Legendary
}

public class UserLevel : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string LevelName { get; private set; } = string.Empty;
    public int LevelNumber { get; private set; }
    public LevelTier Tier { get; private set; }
    public int ExperiencePoints { get; private set; }
    public int ExperienceToNextLevel { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<LevelReward> _rewards = new();
    public IReadOnlyCollection<LevelReward> Rewards => _rewards.AsReadOnly();

    private readonly List<LevelAchievement> _achievements = new();
    public IReadOnlyCollection<LevelAchievement> Achievements => _achievements.AsReadOnly();

    private UserLevel() { }

    public static UserLevel Create(Guid userId, string levelName, int levelNumber, LevelTier tier)
    {
        return new UserLevel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LevelName = levelName,
            LevelNumber = levelNumber,
            Tier = tier,
            ExperiencePoints = 0,
            ExperienceToNextLevel = CalculateExperienceForLevel(levelNumber),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddExperience(int points)
    {
        if (points <= 0) return;

        ExperiencePoints += points;
        
        // Check for level up
        while (ExperiencePoints >= ExperienceToNextLevel)
        {
            LevelUp();
        }

        UpdatedAt = DateTime.UtcNow;
    }

    private void LevelUp()
    {
        ExperiencePoints -= ExperienceToNextLevel;
        LevelNumber++;
        Tier = CalculateTier(LevelNumber);
        ExperienceToNextLevel = CalculateExperienceForLevel(LevelNumber);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddReward(LevelReward reward)
    {
        _rewards.Add(reward);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnlockAchievement(LevelAchievement achievement)
    {
        if (!_achievements.Any(a => a.Id == achievement.Id))
        {
            _achievements.Add(achievement);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    private static LevelTier CalculateTier(int level)
    {
        return level switch
        {
            >= 50 => LevelTier.Legendary,
            >= 40 => LevelTier.Diamond,
            >= 30 => LevelTier.Platinum,
            >= 20 => LevelTier.Gold,
            >= 10 => LevelTier.Silver,
            _ => LevelTier.Bronze
        };
    }

    private static int CalculateExperienceForLevel(int level)
    {
        // Simple formula: 100 * level^1.5
        return (int)(100 * Math.Pow(level, 1.5));
    }

    public float ProgressPercentage
    {
        get
        {
            if (ExperienceToNextLevel == 0) return 0;
            return (float)ExperiencePoints / ExperienceToNextLevel * 100;
        }
    }
}

public class LevelReward : Entity<Guid>
{
    public Guid UserLevelId { get; private set; }
    public string RewardType { get; private set; } = string.Empty;
    public string RewardValue { get; private set; } = string.Empty;
    public DateTime UnlockedAt { get; private set; }

    private LevelReward() { }

    public static LevelReward Create(Guid userLevelId, string rewardType, string rewardValue)
    {
        return new LevelReward
        {
            Id = Guid.NewGuid(),
            UserLevelId = userLevelId,
            RewardType = rewardType,
            RewardValue = rewardValue,
            UnlockedAt = DateTime.UtcNow
        };
    }
}

public class LevelAchievement : Entity<Guid>
{
    public Guid UserLevelId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public DateTime UnlockedAt { get; private set; }

    private LevelAchievement() { }

    public static LevelAchievement Create(Guid userLevelId, string name, string description, string icon)
    {
        return new LevelAchievement
        {
            Id = Guid.NewGuid(),
            UserLevelId = userLevelId,
            Name = name,
            Description = description,
            Icon = icon,
            UnlockedAt = DateTime.UtcNow
        };
    }
}
