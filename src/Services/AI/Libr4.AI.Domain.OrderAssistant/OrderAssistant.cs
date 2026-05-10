using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.AI.Domain.OrderAssistant.Events;

namespace Libr4.AI.Domain.OrderAssistant;

public class OrderSuggestion : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string TaskTitle { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int SuggestedBudget { get; private set; }
    public int SuggestedDuration { get; private set; }
    public List<string> RecommendedFreelancers { get; private set; } = new();
    public float ConfidenceScore { get; private set; }
    public DateTimeOffset SuggestedAt { get; private set; }

    private OrderSuggestion() { }

    public static OrderSuggestion Create(Guid userId, string taskTitle, string description)
    {
        var suggestion = new OrderSuggestion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TaskTitle = taskTitle,
            Description = description,
            SuggestedAt = DateTimeOffset.UtcNow
        };

        suggestion.RaiseDomainEvent(new OrderSuggestedEvent(suggestion.Id, userId, taskTitle, 0, 1, suggestion.SuggestedAt));
        return suggestion;
    }

    public void UpdateSuggestion(int budget, int duration, List<string> freelancers, float confidence, DateTimeOffset now)
    {
        SuggestedBudget = budget;
        SuggestedDuration = duration;
        RecommendedFreelancers = freelancers ?? new List<string>();
        ConfidenceScore = confidence;
        SuggestedAt = now;

        RaiseDomainEvent(new OrderSuggestedEvent(Id, UserId, TaskTitle, budget, duration, now));
    }
}

public class OrderAnalysis
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Requirements { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public DateTimeOffset AnalyzedAt { get; set; }
}
