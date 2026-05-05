using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.AI.Domain.MLResearch.Events;

namespace Libr4.AI.Domain.MLResearch;

public enum ExperimentStatus
{
    Proposed,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum ResearchArea
{
    NLP,
    ComputerVision,
    ReinforcementLearning,
    GenerativeAI,
    AnomalyDetection,
    RecommendationSystems
}

public class MLExperiment : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ResearchArea Area { get; private set; }
    public ExperimentStatus Status { get; private set; }
    public string? ArxivPaperId { get; private set; }
    public string? Dataset { get; private set; }
    public string? ModelArchitecture { get; private set; }
    public float? Accuracy { get; private set; }
    public float? Loss { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private MLExperiment() { }

    public void Start(DateTimeOffset now)
    {
        Status = ExperimentStatus.Running;
        StartedAt = now;
        RaiseDomainEvent(new ExperimentStartedEvent(Id, Title, Area, now));
    }

    public void Complete(float accuracy, float loss, DateTimeOffset now)
    {
        Status = ExperimentStatus.Completed;
        Accuracy = accuracy;
        Loss = loss;
        CompletedAt = now;
        RaiseDomainEvent(new ExperimentCompletedEvent(Id, Title, accuracy, loss, now));
    }

    public void Fail(string reason, DateTimeOffset now)
    {
        Status = ExperimentStatus.Failed;
        CompletedAt = now;
        RaiseDomainEvent(new ExperimentFailedEvent(Id, Title, reason, now));
    }
}

public class ArxivPaperSuggestion
{
    public Guid Id { get; set; }
    public string PaperId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
    public ResearchArea Area { get; set; }
    public float RelevanceScore { get; private set; }
    public DateTimeOffset SuggestedAt { get; set; }

    public void UpdateRelevanceScore(float score)
    {
        RelevanceScore = score;
    }
}
