using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.AI.Domain.SkillScoring.Events;

namespace Libr4.AI.Domain.SkillScoring;

public class UserSkillScore : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string SkillName { get; private set; } = string.Empty;
    public float ProficiencyScore { get; private set; } // 0-100
    public string ProficiencyLevel { get; private set; } = string.Empty; // Beginner, Intermediate, Advanced, Expert
    public int UsageCount { get; private set; }
    public DateTimeOffset LastAssessedAt { get; private set; }

    private UserSkillScore() { }

    public void UpdateScore(float newScore, DateTimeOffset now)
    {
        ProficiencyScore = newScore;
        ProficiencyLevel = newScore switch
        {
            < 25 => "Beginner",
            < 50 => "Intermediate",
            < 75 => "Advanced",
            _ => "Expert"
        };
        UsageCount++;
        LastAssessedAt = now;
        RaiseDomainEvent(new SkillScoreUpdatedEvent(Id, UserId, SkillName, newScore, ProficiencyLevel, now));
    }
}

public class SkillAssessment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<string> Skills { get; set; } = new();
    public float OverallScore { get; set; }
    public List<string> RecommendedImprovements { get; set; } = new();
    public DateTimeOffset AssessedAt { get; set; }
}
