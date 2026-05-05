using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.AI.Domain.TaskRecommendations.Events;

namespace Libr4.AI.Domain.TaskRecommendations;

public class TaskRecommendation : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid TaskId { get; private set; }
    public string TaskTitle { get; private set; } = string.Empty;
    public float MatchScore { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public List<string> MatchingSkills { get; private set; } = new();
    public DateTimeOffset RecommendedAt { get; private set; }

    private TaskRecommendation() { }

    public void Recommend(float matchScore, string reason, List<string> matchingSkills, DateTimeOffset now)
    {
        MatchScore = matchScore;
        Reason = reason;
        MatchingSkills = matchingSkills;
        RecommendedAt = now;
        RaiseDomainEvent(new TaskRecommendedEvent(Id, UserId, TaskId, matchScore, now));
    }
}

public class UserProfileForRecommendations
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<string> Interests { get; set; } = new();
    public float AverageRating { get; set; }
    public int CompletedTasks { get; set; }
}
