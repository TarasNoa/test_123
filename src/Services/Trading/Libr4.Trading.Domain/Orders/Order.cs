using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Trading.Domain.Orders;

public enum OrderType
{
    Market,     // Execute at current market price
    Limit,      // Execute at specified price or better
    StopLoss,   // Trigger at stop price, execute as market
    TakeProfit  // Trigger at profit price, execute as market
}

public enum OrderSide
{
    Buy,
    Sell
}

public enum OrderStatus
{
    Pending,      // New order, not yet in market
    Open,         // Active in market (limit orders)
    Filled,       // Fully executed
    PartiallyFilled, // Partially executed
    Cancelled,    // Cancelled by user
    Rejected,     // Rejected by system/exchange
    Expired       // Time limit expired
}

public enum TimeInForce
{
    GTC, // Good Till Cancelled
    IOC, // Immediate Or Cancel
    FOK  // Fill Or Kill
}

public class Order : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid AssetId { get; private set; }
    public string AssetSymbol { get; private set; } = string.Empty;
    public OrderType Type { get; private set; }
    public OrderSide Side { get; private set; }
    public OrderStatus Status { get; private set; }
    public TimeInForce TimeInForce { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal? Price { get; private set; } // Required for limit orders
    public decimal? StopPrice { get; private set; } // Required for stop orders

    public decimal FilledQuantity { get; private set; }
    public decimal? AverageFillPrice { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? ExecutedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    private readonly List<Trade> _trades = new();
    public IReadOnlyCollection<Trade> Trades => _trades.AsReadOnly();

    private Order() { } // EF Core

    public Order(
        Guid id,
        Guid userId,
        Guid assetId,
        string assetSymbol,
        OrderType type,
        OrderSide side,
        decimal quantity,
        decimal? price = null,
        decimal? stopPrice = null,
        TimeInForce tif = TimeInForce.GTC,
        DateTime? expiresAt = null) : base(id)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (type == OrderType.Limit && !price.HasValue)
            throw new ArgumentException("Limit orders require a price", nameof(price));
        if ((type == OrderType.StopLoss || type == OrderType.TakeProfit) && !stopPrice.HasValue)
            throw new ArgumentException("Stop orders require a stop price", nameof(stopPrice));

        UserId = userId;
        AssetId = assetId;
        AssetSymbol = assetSymbol.ToUpper();
        Type = type;
        Side = side;
        Quantity = quantity;
        Price = price;
        StopPrice = stopPrice;
        TimeInForce = tif;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public void Submit()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Order must be in Pending state to submit");

        Status = Type == OrderType.Market ? OrderStatus.Open : OrderStatus.Open;
    }

    public void Fill(decimal fillQuantity, decimal fillPrice)
    {
        if (fillQuantity <= 0)
            throw new ArgumentException("Fill quantity must be positive", nameof(fillQuantity));
        if (fillQuantity > Quantity - FilledQuantity)
            throw new ArgumentException("Fill quantity exceeds remaining quantity");

        FilledQuantity += fillQuantity;

        // Update average fill price
        var totalValue = (AverageFillPrice ?? 0) * (FilledQuantity - fillQuantity) + fillPrice * fillQuantity;
        AverageFillPrice = totalValue / FilledQuantity;

        // Add trade record
        _trades.Add(new Trade(
            Guid.NewGuid(),
            Id,
            UserId,
            AssetId,
            AssetSymbol,
            Side,
            fillQuantity,
            fillPrice,
            fillQuantity * fillPrice));

        if (FilledQuantity >= Quantity)
        {
            Status = OrderStatus.Filled;
            ExecutedAt = DateTime.UtcNow;
        }
        else
        {
            Status = OrderStatus.PartiallyFilled;
        }
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Filled || Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel filled or already cancelled order");

        Status = OrderStatus.Cancelled;
    }

    public void Reject(string reason)
    {
        if (Status == OrderStatus.Filled)
            throw new InvalidOperationException("Cannot reject filled order");

        Status = OrderStatus.Rejected;
    }

    public bool ShouldTriggerStop(decimal currentPrice)
    {
        if (Type != OrderType.StopLoss && Type != OrderType.TakeProfit)
            return false;

        return Side == OrderSide.Buy
            ? currentPrice <= StopPrice // Buy stop triggers when price drops to stop
            : currentPrice >= StopPrice; // Sell stop triggers when price rises to stop
    }
}
