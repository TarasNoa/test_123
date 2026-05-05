using Libr4.Shared.Kernel.Domain;

namespace Libr4.Trading.Domain.Bots;

public enum BotStatus
{
    Stopped,
    Running,
    Paused,
    Error,
    Disabled
}

public enum BotType
{
    Grid,
    Arbitrage,
    MarketMaking,
    Momentum,
    MeanReversion,
    Custom
}

public enum SignalType
{
    Buy,
    Sell,
    Hold
}

public class TradingBot : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public BotType Type { get; private set; }
    public BotStatus Status { get; private set; }
    
    // Configuration
    public Dictionary<string, object> Config { get; private set; } = new();
    
    // Performance tracking
    public decimal TotalProfit { get; private set; }
    public decimal TotalLoss { get; private set; }
    public int TotalTrades { get; private set; }
    public int WinningTrades { get; private set; }
    public decimal WinRate => TotalTrades > 0 ? (decimal)WinningTrades / TotalTrades * 100 : 0;
    
    // Risk management
    public decimal MaxDrawdown { get; private set; }
    public decimal RiskPerTrade { get; private set; } = 0.02m; // Default 2% risk
    
    // Timing
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? StoppedAt { get; private set; }
    public DateTime? LastTradeAt { get; private set; }

    private readonly List<BotTrade> _trades = new();
    public IReadOnlyCollection<BotTrade> Trades => _trades.AsReadOnly();

    private TradingBot() { }

    public static TradingBot Create(
        Guid userId,
        string name,
        string description,
        BotType type,
        Dictionary<string, object>? config = null)
    {
        return new TradingBot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description,
            Type = type,
            Status = BotStatus.Stopped,
            Config = config ?? new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Start()
    {
        if (Status != BotStatus.Stopped && Status != BotStatus.Paused)
            throw new InvalidOperationException($"Cannot start bot in {Status} state");
        
        Status = BotStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    public void Stop()
    {
        if (Status != BotStatus.Running)
            throw new InvalidOperationException($"Cannot stop bot in {Status} state");
        
        Status = BotStatus.Stopped;
        StoppedAt = DateTime.UtcNow;
    }

    public void Pause()
    {
        if (Status != BotStatus.Running)
            throw new InvalidOperationException($"Cannot pause bot in {Status} state");
        
        Status = BotStatus.Paused;
    }

    public void Resume()
    {
        if (Status != BotStatus.Paused)
            throw new InvalidOperationException($"Cannot resume bot in {Status} state");
        
        Status = BotStatus.Running;
    }

    public void RecordTrade(BotTrade trade)
    {
        _trades.Add(trade);
        TotalTrades++;
        LastTradeAt = DateTime.UtcNow;
        
        if (trade.Profit > 0)
        {
            TotalProfit += trade.Profit;
            WinningTrades++;
        }
        else if (trade.Profit < 0)
        {
            TotalLoss += Math.Abs(trade.Profit);
        }

        // Update max drawdown
        var currentDrawdown = CalculateDrawdown();
        if (currentDrawdown > MaxDrawdown)
            MaxDrawdown = currentDrawdown;
    }

    public void UpdateConfig(string key, object value)
    {
        Config[key] = value;
    }

    public void SetRiskPerTrade(decimal risk)
    {
        if (risk <= 0 || risk > 1)
            throw new ArgumentException("Risk must be between 0 and 1", nameof(risk));
        
        RiskPerTrade = risk;
    }

    private decimal CalculateDrawdown()
    {
        if (_trades.Count == 0)
            return 0;
        
        var peak = 0m;
        var maxDrawdown = 0m;
        var currentBalance = 0m;
        
        foreach (var trade in _trades.OrderBy(t => t.ExecutedAt))
        {
            currentBalance += trade.Profit;
            if (currentBalance > peak)
                peak = currentBalance;
            
            var drawdown = peak > 0 ? (peak - currentBalance) / peak * 100 : 0;
            if (drawdown > maxDrawdown)
                maxDrawdown = drawdown;
        }
        
        return maxDrawdown;
    }
}

public class BotTrade : Entity<Guid>
{
    public Guid BotId { get; private set; }
    public string AssetSymbol { get; private set; } = string.Empty;
    public SignalType Signal { get; private set; }
    public decimal EntryPrice { get; private set; }
    public decimal? ExitPrice { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Profit { get; private set; }
    public DateTime ExecutedAt { get; private set; }
    public string? Reason { get; private set; }

    private BotTrade() { }

    public static BotTrade Create(
        Guid botId,
        string assetSymbol,
        SignalType signal,
        decimal entryPrice,
        decimal quantity,
        string? reason = null)
    {
        return new BotTrade
        {
            Id = Guid.NewGuid(),
            BotId = botId,
            AssetSymbol = assetSymbol,
            Signal = signal,
            EntryPrice = entryPrice,
            Quantity = quantity,
            ExecutedAt = DateTime.UtcNow,
            Reason = reason
        };
    }

    public void Close(decimal exitPrice)
    {
        ExitPrice = exitPrice;
        
        // Calculate profit
        if (Signal == SignalType.Buy)
            Profit = (exitPrice - EntryPrice) * Quantity;
        else
            Profit = (EntryPrice - exitPrice) * Quantity;
    }
}
