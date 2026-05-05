using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AIOptimization;

public enum OptimizationStrategy { PromptCaching, ModelRouting, BatchProcessing, Compression, Quantization }
public enum CostTier { Economy, Standard, Premium, Enterprise }

public class OptimizationConfig
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public OptimizationStrategy Strategy { get; set; }
    public decimal CostLimit { get; set; }
    public int LatencyLimitMs { get; set; }
    public CostTier Tier { get; set; } = CostTier.Standard;
    public bool EnableCaching { get; set; } = true;
    public bool EnableModelRouting { get; set; } = true;
    public Dictionary<string, object> Rules { get; set; } = new Dictionary<string, object>();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class PromptCache
{
    public Guid Id { get; set; }
    public string PromptHash { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int TokensSaved { get; set; }
    public decimal CostSaved { get; set; }
    public int HitCount { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ModelRoute
{
    public Guid Id { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string PrimaryModel { get; set; } = string.Empty;
    public string? FallbackModel { get; set; }
    public int MaxTokens { get; set; }
    public float MaxCost { get; set; }
    public bool IsActive { get; set; } = true;
}
