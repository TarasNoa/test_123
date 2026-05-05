using System;
using System.Collections.Generic;

namespace Libr4.Payments.Domain.Pricing;

public enum PricingStrategy { Fixed, Dynamic, Competitive, ValueBased, Tiered }
public enum MarketCondition { HighDemand, Normal, LowDemand, Competitive }
public enum BidStrategyType { Aggressive, Balanced, Conservative }

public class PriceRecommendation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TaskId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? ProjectType { get; set; }
    public float? ComplexityScore { get; set; }
    public float? EstimatedHours { get; set; }
    public MarketCondition MarketCondition { get; set; } = MarketCondition.Normal;
    public float? CompetitorAvgPrice { get; set; }
    public float? MarketDemandScore { get; set; }
    public float RecommendedMinPrice { get; set; }
    public float RecommendedMaxPrice { get; set; }
    public float OptimalPrice { get; set; }
    public float? ConfidenceScore { get; set; }
    public Dictionary<string, object> PricingFactors { get; set; } = [];
    public PricingStrategy? SuggestedStrategy { get; set; }
    public string? AIReasoning { get; set; }
    public int SimilarProjectsAnalyzed { get; set; }
    public bool WasAccepted { get; set; }
    public float? FinalPriceSet { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
    public void Accept(float finalPrice) { WasAccepted = true; FinalPriceSet = finalPrice; }
}

public class MarketRate
{
    public Guid Id { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public float HourlyRateMin { get; set; }
    public float HourlyRateMax { get; set; }
    public float HourlyRateAvg { get; set; }
    public float HourlyRateMedian { get; set; }
    public float? ProjectRateMin { get; set; }
    public float? ProjectRateMax { get; set; }
    public float? ProjectRateAvg { get; set; }
    public int SampleSize { get; set; }
    public float? BeginnerRateAvg { get; set; }
    public float? IntermediateRateAvg { get; set; }
    public float? AdvancedRateAvg { get; set; }
    public float? ExpertRateAvg { get; set; }
    public float? TrendPercentage { get; set; }
    public float? DemandScore { get; set; }
    public string Region { get; set; } = "global";
    public string Currency { get; set; } = "USD";
    public DateTimeOffset LastUpdated { get; set; }
    public List<string> DataSources { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class BidOptimization
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TaskId { get; set; }
    public float ProposedBid { get; set; }
    public float? WinProbability { get; set; }
    public float? CompetitivenessScore { get; set; }
    public float? ValueScore { get; set; }
    public int? EstimatedCompetitors { get; set; }
    public float? EstimatedAvgCompetitorBid { get; set; }
    public float? EstimatedLowestBid { get; set; }
    public float? RecommendedBid { get; set; }
    public string? RecommendedTimeline { get; set; }
    public List<string> SuggestedAdjustments { get; set; } = [];
    public BidStrategyType? StrategyType { get; set; }
    public string? AIInsights { get; set; }
    public bool WasBidSubmitted { get; set; }
    public float? FinalBidAmount { get; set; }
    public bool? DidWin { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class PricingExperiment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PricingStrategy StrategyA { get; set; }
    public PricingStrategy StrategyB { get; set; }
    public Dictionary<string, object> StrategyAConfig { get; set; } = [];
    public Dictionary<string, object> StrategyBConfig { get; set; } = [];
    public Dictionary<string, object> SkillFilter { get; set; } = [];
    public Dictionary<string, object> UserFilter { get; set; } = [];
    public int StrategyAConversions { get; set; }
    public int StrategyBConversions { get; set; }
    public float StrategyARevenue { get; set; }
    public float StrategyBRevenue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? WinningStrategy { get; set; } // 'A' or 'B'
    public float? ConfidenceLevel { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public string DetermineWinner()
    {
        if (StrategyARevenue > StrategyBRevenue) return "A";
        if (StrategyBRevenue > StrategyARevenue) return "B";
        return "tie";
    }
}

public class DynamicPricingRule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RuleType { get; set; } = string.Empty; // time_based, demand_based, user_based
    public Dictionary<string, object> Conditions { get; set; } = [];
    public float PriceMultiplier { get; set; } = 1.0f;
    public float FixedAdjustment { get; set; }
    public int Priority { get; set; }
    public float? MinPriceCap { get; set; }
    public float? MaxPriceCap { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> AppliesToSkills { get; set; } = [];
    public List<string> AppliesToCategories { get; set; } = [];
    public int TimesApplied { get; set; }
    public float RevenueImpact { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public float CalculatePrice(float basePrice)
    {
        var adjusted = basePrice * PriceMultiplier + FixedAdjustment;
        if (MinPriceCap.HasValue && adjusted < MinPriceCap.Value) adjusted = MinPriceCap.Value;
        if (MaxPriceCap.HasValue && adjusted > MaxPriceCap.Value) adjusted = MaxPriceCap.Value;
        return adjusted;
    }
}

public class Discount
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal PercentOff { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int? MaxUses { get; set; }
    public int UsageCount { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public bool CanUse() => IsActive && UsageCount < (MaxUses ?? int.MaxValue) && DateTimeOffset.UtcNow < ExpiresAt;
}
