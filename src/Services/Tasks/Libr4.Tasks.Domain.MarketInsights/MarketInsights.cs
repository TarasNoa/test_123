using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Tasks.Domain.MarketInsights.Events;

namespace Libr4.Tasks.Domain.MarketInsights;

public class MarketInsight : AggregateRoot<Guid>
{
    public string Category { get; private set; } = string.Empty;
    public string InsightType { get; private set; } = string.Empty; // Pricing, Demand, Skills, Competition
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public List<string> DataPoints { get; private set; } = new();
    public string Recommendation { get; private set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; private set; }

    private MarketInsight() { }

    public void Generate(string category, string insightType, string title, string description, List<string> dataPoints, string recommendation, DateTimeOffset now)
    {
        Category = category;
        InsightType = insightType;
        Title = title;
        Description = description;
        DataPoints = dataPoints;
        Recommendation = recommendation;
        GeneratedAt = now;
        RaiseDomainEvent(new MarketInsightGeneratedEvent(Id, category, insightType, now));
    }
}

public class MarketTrend
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TrendDirection { get; set; } = string.Empty; // Increasing, Decreasing, Stable
    public float ChangePercentage { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public DateTimeOffset ObservedAt { get; set; }
}
