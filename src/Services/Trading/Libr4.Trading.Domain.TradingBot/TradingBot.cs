using System;
using System.Collections.Generic;

namespace Libr4.Trading.Domain.TradingBot;

public enum TradingStrategyType { MovingAverage, RSI, MACD, BollingerBands, AIEnhanced, Custom }
public enum TradingBotStatus { Active, Paused, Stopped, Error }
public enum TradeSide { Buy, Sell }

public class TradingStrategy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TradingStrategyType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = [];
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class TradingBot
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TradingStrategyType StrategyType { get; set; }
    public Dictionary<string, object> StrategyConfig { get; set; } = [];
    public TradingBotStatus Status { get; set; } = TradingBotStatus.Stopped;
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal TotalProfit { get; set; }
    public int TotalTrades { get; set; }
    public int SuccessfulTrades { get; set; }
    public List<TradingBotTrade> Trades { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public decimal WinRate => TotalTrades > 0 ? (decimal)SuccessfulTrades / TotalTrades * 100 : 0;
    public decimal ROI => InitialBalance > 0 ? (CurrentBalance - InitialBalance) / InitialBalance * 100 : 0;

    public void Start(DateTimeOffset now) { Status = TradingBotStatus.Active; UpdatedAt = now; }
    public void Pause(DateTimeOffset now) { Status = TradingBotStatus.Paused; UpdatedAt = now; }
    public void Stop(DateTimeOffset now) { Status = TradingBotStatus.Stopped; UpdatedAt = now; }

    public void RecordTrade(TradingBotTrade trade, DateTimeOffset now)
    {
        Trades.Add(trade);
        TotalTrades++;
        if (trade.ProfitLoss > 0) SuccessfulTrades++;
        TotalProfit += trade.ProfitLoss ?? 0;
        CurrentBalance += trade.ProfitLoss ?? 0;
        UpdatedAt = now;
    }
}

public class TradingBotTrade
{
    public Guid Id { get; set; }
    public Guid BotId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public TradeSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? ProfitLoss { get; set; }
    public float? AISentimentScore { get; set; }
    public Dictionary<string, object> TechnicalSignals { get; set; } = [];
    public DateTimeOffset ExecutedAt { get; set; }
}

public class BacktestResult
{
    public Guid Id { get; set; }
    public Guid BotId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal FinalCapital { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public Dictionary<string, object> DetailedStats { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
}
