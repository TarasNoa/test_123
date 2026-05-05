using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AIMonitoring;

public enum AIModelType { TextGeneration, SentimentAnalysis, Translation, Summarization, QuestionAnswering, NER, ImageClassification, Embeddings }

public class AIModelUsage
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public AIModelType ModelType { get; set; }
    public int TokensUsed { get; set; }
    public decimal Cost { get; set; }
    public Dictionary<string, object> RequestData { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> ResponseData { get; set; } = new Dictionary<string, object>();
    public int? LatencyMs { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class AIEmbedding
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<float> EmbeddingVector { get; set; } = new List<float>();
    public string ModelName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class PlatformAnalytics
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, object> ActionData { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> AIEnhancementsUsed { get; set; } = new Dictionary<string, object>();
    public DateTimeOffset CreatedAt { get; set; }
}

public class CostAggregation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset Period { get; set; }
    public decimal TotalCost { get; set; }
    public int TotalTokens { get; set; }
    public int TotalRequests { get; set; }
    public Dictionary<string, decimal> CostByModel { get; set; } = new Dictionary<string, decimal>();
    public DateTimeOffset CreatedAt { get; set; }
}

public class Metric
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Unit { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    public DateTimeOffset Timestamp { get; set; }
}
