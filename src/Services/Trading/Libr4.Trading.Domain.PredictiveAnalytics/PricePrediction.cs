using System;
using System.Collections.Generic;

namespace Libr4.Trading.Domain.PredictiveAnalytics;

public enum ModelType { LSTM, Transformer, XGBoost, RandomForest, Ensemble }
public enum PredictionHorizon { Short, Medium, Long }

public class PricePrediction
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public ModelType ModelType { get; set; }
    public PredictionHorizon Horizon { get; set; }
    public decimal PredictedPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal ExpectedChange { get; set; }
    public float Confidence { get; set; }
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public DateTimeOffset PredictionFor { get; set; }
    public List<string> Factors { get; set; } = new List<string>();
    public Dictionary<string, object> FeatureImportance { get; set; } = new Dictionary<string, object>();
    public bool WasAccurate { get; set; }
    public decimal? ActualPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ModelPerformance
{
    public Guid Id { get; set; }
    public ModelType ModelType { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int TotalPredictions { get; set; }
    public int AccuratePredictions { get; set; }
    public decimal AverageError { get; set; }
    public decimal MaxError { get; set; }
    public decimal SharpeRatio { get; set; }
    public DateTimeOffset LastEvaluatedAt { get; set; }

    public float Accuracy => TotalPredictions > 0 ? (float)AccuratePredictions / TotalPredictions * 100 : 0;
}

public class SentimentAnalysis
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal SentimentScore { get; set; } // -1 to 1
    public int PositiveMentions { get; set; }
    public int NegativeMentions { get; set; }
    public int NeutralMentions { get; set; }
    public List<string> TopKeywords { get; set; } = new List<string>();
    public List<string> DataSources { get; set; } = new List<string>();
    public DateTimeOffset AnalyzedAt { get; set; }
}
