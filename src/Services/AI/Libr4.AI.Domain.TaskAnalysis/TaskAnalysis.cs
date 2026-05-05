using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.AI.Domain.TaskAnalysis.Events;

namespace Libr4.AI.Domain.TaskAnalysis;

public class TaskComplexityAnalysis : AggregateRoot<Guid>
{
    public Guid TaskId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int ComplexityScore { get; private set; } // 1-10
    public string ComplexityLevel { get; private set; } = string.Empty; // Low, Medium, High
    public int EstimatedHours { get; private set; }
    public int EstimatedCost { get; private set; }
    public List<string> RequiredSkills { get; private set; } = new();
    public List<string> RiskFactors { get; private set; } = new();
    public DateTimeOffset AnalyzedAt { get; private set; }

    private TaskComplexityAnalysis() { }

    public void AnalyzeComplexity(int complexityScore, int estimatedHours, int estimatedCost, List<string> requiredSkills, List<string> riskFactors, DateTimeOffset now)
    {
        ComplexityScore = complexityScore;
        EstimatedHours = estimatedHours;
        EstimatedCost = estimatedCost;
        RequiredSkills = requiredSkills;
        RiskFactors = riskFactors;
        
        ComplexityLevel = complexityScore switch
        {
            <= 3 => "Low",
            <= 7 => "Medium",
            _ => "High"
        };
        
        AnalyzedAt = now;
        RaiseDomainEvent(new TaskComplexityAnalyzedEvent(Id, TaskId, ComplexityScore, ComplexityLevel, estimatedHours, estimatedCost, now));
    }
}

public class ApplicationAnalysis
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid TaskId { get; set; }
    public Guid ApplicantId { get; set; }
    public float MatchScore { get; private set; }
    public string Recommendation { get; private set; } = string.Empty;
    public List<string> Strengths { get; private set; } = new();
    public List<string> Weaknesses { get; private set; } = new();
    public DateTimeOffset AnalyzedAt { get; set; }

    public void UpdateAnalysis(float matchScore, string recommendation, List<string> strengths, List<string> weaknesses)
    {
        MatchScore = matchScore;
        Recommendation = recommendation;
        Strengths = strengths;
        Weaknesses = weaknesses;
    }
}
