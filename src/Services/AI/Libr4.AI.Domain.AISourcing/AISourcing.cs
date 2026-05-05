using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AISourcing;

public enum ExperienceLevel { Junior, Mid, Senior, Expert }
public enum CampaignStatus { Draft, Active, Paused, Completed }
public enum AlertFrequency { Daily, Weekly }

public class TalentProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? AISummary { get; set; }
    public List<string> SkillTags { get; set; } = [];
    public ExperienceLevel ExperienceLevel { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int? AvailabilityHours { get; set; }
    public float? PreferredRateMin { get; set; }
    public float? PreferredRateMax { get; set; }
    public List<string> PreferredProjectTypes { get; set; } = [];
    public List<string> PreferredIndustries { get; set; } = [];
    public Dictionary<string, object> WorkPreferences { get; set; } = [];
    public float? QualityScore { get; set; }
    public float? ReliabilityScore { get; set; }
    public float? CommunicationScore { get; set; }
    public DateTimeOffset? LastEnrichedAt { get; set; }
    public float? ProfileCompleteness { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public float OverallScore => ((QualityScore ?? 0) + (ReliabilityScore ?? 0) + (CommunicationScore ?? 0)) / 3;
}

public class TalentSearch
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SearchQuery { get; set; }
    public Dictionary<string, object> Filters { get; set; } = [];
    public bool UseAIMatching { get; set; } = true;
    public float SimilarityThreshold { get; set; } = 0.7f;
    public bool AlertOnNewMatches { get; set; }
    public AlertFrequency? AlertFrequency { get; set; }
    public int LastMatchCount { get; set; }
    public DateTimeOffset? LastExecutedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class TalentRecommendation
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid TalentId { get; set; }
    public float OverallMatchScore { get; set; }
    public float? SkillMatchScore { get; set; }
    public float? ExperienceMatchScore { get; set; }
    public float? AvailabilityMatchScore { get; set; }
    public float? RateMatchScore { get; set; }
    public string? MatchReasoning { get; set; }
    public List<string> Strengths { get; set; } = [];
    public List<string> Concerns { get; set; } = [];
    public bool WasContacted { get; set; }
    public bool WasHired { get; set; }
    public bool WasRejected { get; set; }
    public string? ClientFeedback { get; set; }
    public int? FeedbackRating { get; set; }
    public DateTimeOffset RecommendedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
}

public class OutreachCampaign
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? TargetCount { get; set; }
    public List<string> TargetSkills { get; set; } = [];
    public Dictionary<string, object> TargetFilters { get; set; } = [];
    public string MessageTemplate { get; set; } = string.Empty;
    public bool UseAIPersonalization { get; set; } = true;
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public int TalentsTargeted { get; set; }
    public int MessagesSent { get; set; }
    public int ResponsesReceived { get; set; }
    public int Conversions { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public float ResponseRate => MessagesSent > 0 ? (float)ResponsesReceived / MessagesSent * 100 : 0;
    public float ConversionRate => MessagesSent > 0 ? (float)Conversions / MessagesSent * 100 : 0;

    public void Start(DateTimeOffset now) { Status = CampaignStatus.Active; StartedAt = now; }
    public void Complete(DateTimeOffset now) { Status = CampaignStatus.Completed; CompletedAt = now; }
}
