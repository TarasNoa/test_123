using System;
using System.Collections.Generic;

namespace Libr4.Trading.Domain.MarketDataExtended;

public enum CandleInterval { Min1, Min5, Min15, Min30, Hour1, Hour4, Day1, Week1, Month1 }

public class OrderBook
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = "binance";
    public List<OrderBookEntry> Bids { get; set; } = new List<OrderBookEntry>();
    public List<OrderBookEntry> Asks { get; set; } = new List<OrderBookEntry>();
    public decimal BestBid => Bids.Count > 0 ? Bids[0].Price : 0;
    public decimal BestAsk => Asks.Count > 0 ? Asks[0].Price : 0;
    public decimal Spread => BestAsk - BestBid;
    public long Timestamp { get; set; }
}

public class OrderBookEntry
{
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal Total => Price * Quantity;
}

public class Trade
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string TradeId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public bool IsBuyerMaker { get; set; }
    public long Timestamp { get; set; }
}

public class Candle
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public CandleInterval Interval { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public int TradeCount { get; set; }
    public long OpenTime { get; set; }
    public long CloseTime { get; set; }

    public bool IsBullish => Close > Open;
    public decimal Change => Close - Open;
    public decimal ChangePercent => Open != 0 ? (Change / Open) * 100 : 0;
}

public class MarketStats
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal PriceChange24h { get; set; }
    public decimal PriceChangePercent24h { get; set; }
    public decimal WeightedAvgPrice { get; set; }
    public decimal High24h { get; set; }
    public decimal Low24h { get; set; }
    public decimal Volume24h { get; set; }
    public decimal QuoteVolume24h { get; set; }
    public long OpenTime { get; set; }
    public long CloseTime { get; set; }
    public int TradeCount { get; set; }
}
