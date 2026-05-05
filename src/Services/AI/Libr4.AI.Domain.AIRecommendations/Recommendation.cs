using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AIRecommendations;

public enum RecommendationType { Task, Freelancer, Project, Skill, Learning, Team }
public enum RecommendationPriority { Low, Medium, High, Critical }

public class UserRecommendation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public RecommendationType RecommendationType { get; set; }
    public Dictionary<string, object> RecommendationData { get; set; } = new Dictionary<string, object>();
    public RecommendationPriority Priority { get; set; } = RecommendationPriority.Medium;
    public float Score { get; set; }
    public string? Reasoning { get; set; }
    public bool IsRead { get; set; }
    public bool WasActedOn { get; set; }
    public string? UserFeedback { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
    public void MarkAsRead() => IsRead = true;
    public void MarkActedOn() => WasActedOn = true;
}

public class FreelancerMatch
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid FreelancerId { get; set; }
    public float OverallScore { get; set; }
    public float SkillMatchScore { get; set; }
    public float ExperienceScore { get; set; }
    public float RatingScore { get; set; }
    public float AvailabilityScore { get; set; }
    public float PriceFitScore { get; set; }
    public List<string> MatchingSkills { get; set; } = new List<string>();
    public List<string> MissingSkills { get; set; } = new List<string>();
    public string? AIAnalysis { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Recommendation
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Score { get; set; }
}
