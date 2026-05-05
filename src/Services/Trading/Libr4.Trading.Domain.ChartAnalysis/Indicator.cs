using System;
using System.Collections.Generic;
using Libr4.Shared.Kernel.Domain;
using Libr4.Trading.Domain.ChartAnalysis.Events;

namespace Libr4.Trading.Domain.ChartAnalysis;

public enum IndicatorType { SMA, EMA, RSI, MACD, BollingerBands, Stochastic, ATR, OBV, Volume, Ichimoku }
public enum PatternType { HeadAndShoulders, DoubleTop, DoubleBottom, Triangle, Flag, Wedge, CupAndHandle, Channel }
public enum Trend { Bullish, Bearish, Sideways }

public class TechnicalIndicator : AggregateRoot<Guid>
{
    public string Symbol { get; private set; } = string.Empty;
    public string TimeFrame { get; private set; } = "1h";
    public IndicatorType Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public Dictionary<string, decimal> Values { get; private set; } = new Dictionary<string, decimal>();
    public Dictionary<string, object> Parameters { get; private set; } = new Dictionary<string, object>();
    public string? Signal { get; private set; } // buy, sell, neutral
    public float? Confidence { get; private set; }
    public DateTimeOffset CalculatedAt { get; private set; }

    private TechnicalIndicator() { }

    public void UpdateValue(decimal newValue, string? newSignal, float? newConfidence, DateTimeOffset now)
    {
        Value = newValue;
        Signal = newSignal;
        Confidence = newConfidence;
        CalculatedAt = now;
        RaiseDomainEvent(new IndicatorValueUpdatedEvent(Id, Symbol, newValue, newSignal, now));
    }
}

public class ChartPattern : AggregateRoot<Guid>
{
    public string Symbol { get; private set; } = string.Empty;
    public string TimeFrame { get; private set; } = "1h";
    public PatternType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public float Confidence { get; private set; }
    public DateTimeOffset DetectedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public decimal? TargetPrice { get; private set; }
    public decimal? StopLossPrice { get; private set; }
    public Trend ExpectedTrend { get; private set; }

    private ChartPattern() { }

    public void Complete(decimal actualPrice, DateTimeOffset now)
    {
        CompletedAt = now;
        RaiseDomainEvent(new PatternCompletedEvent(Id, Symbol, Type, actualPrice, now));
    }
}

public class MarketAnalysis : AggregateRoot<Guid>
{
    public string Symbol { get; private set; } = string.Empty;
    public Trend OverallTrend { get; private set; }
    public List<TechnicalIndicator> Indicators { get; private set; } = new List<TechnicalIndicator>();
    public List<ChartPattern> Patterns { get; private set; } = new List<ChartPattern>();
    public string? AISummary { get; private set; }
    public float? BullishScore { get; private set; }
    public float? BearishScore { get; private set; }
    public DateTimeOffset AnalyzedAt { get; private set; }

    private MarketAnalysis() { }

    public void UpdateAnalysis(Trend newTrend, float? bullishScore, float? bearishScore, string? aiSummary, DateTimeOffset now)
    {
        OverallTrend = newTrend;
        BullishScore = bullishScore;
        BearishScore = bearishScore;
        AISummary = aiSummary;
        AnalyzedAt = now;
        RaiseDomainEvent(new MarketAnalysisUpdatedEvent(Id, Symbol, newTrend, bullishScore, bearishScore, now));
    }
}
