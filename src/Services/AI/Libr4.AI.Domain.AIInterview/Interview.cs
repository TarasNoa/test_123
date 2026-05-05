using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AIInterview;

public enum InterviewStatus { Scheduled, InProgress, Completed, Cancelled }
public enum QuestionType { Technical, Behavioral, SystemDesign, Coding, ProblemSolving }

public class Interview
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Guid? RecruiterId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> SkillsToAssess { get; set; } = new List<string>();
    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
    public List<InterviewQuestion> Questions { get; set; } = new List<InterviewQuestion>();
    public int? Duration { get; set; }
    public float? OverallScore { get; set; }
    public string? AIAssessment { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class InterviewQuestion
{
    public Guid Id { get; set; }
    public Guid InterviewId { get; set; }
    public int Order { get; set; }
    public QuestionType Type { get; set; }
    public string Question { get; set; } = string.Empty;
    public string? ExpectedAnswer { get; set; }
    public string? CandidateAnswer { get; set; }
    public float? Score { get; set; }
    public string? AIFeedback { get; set; }
    public int? TimeSpentSeconds { get; set; }
}
