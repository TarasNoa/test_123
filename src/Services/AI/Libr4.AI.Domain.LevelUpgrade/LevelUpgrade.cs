using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.AI.Domain.LevelUpgrade.Events;

namespace Libr4.AI.Domain.LevelUpgrade;

public class LevelUpgradeSuggestion : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string CurrentLevel { get; private set; } = string.Empty;
    public string SuggestedLevel { get; private set; } = string.Empty;
    public List<string> Requirements { get; private set; } = new();
    public List<string> Achievements { get; private set; } = new();
    public float ReadinessScore { get; private set; }
    public DateTimeOffset SuggestedAt { get; private set; }

    private LevelUpgradeSuggestion() { }

    public void SuggestUpgrade(string suggestedLevel, List<string> requirements, List<string> achievements, float readinessScore, DateTimeOffset now)
    {
        SuggestedLevel = suggestedLevel;
        Requirements = requirements;
        Achievements = achievements;
        ReadinessScore = readinessScore;
        SuggestedAt = now;
        RaiseDomainEvent(new LevelUpgradeSuggestedEvent(Id, UserId, CurrentLevel, suggestedLevel, readinessScore, now));
    }
}

public class UserLevelProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Level { get; set; } = string.Empty;
    public int XP { get; set; }
    public int XPToNextLevel { get; set; }
    public List<string> UnlockedAchievements { get; set; } = new();
    public List<string> CompletedCourses { get; set; } = new();
    public DateTimeOffset LastUpdated { get; set; }
}
