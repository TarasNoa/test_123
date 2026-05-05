using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Trading.Domain.Orders;

public class Trade : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AssetId { get; private set; }
    public string AssetSymbol { get; private set; } = string.Empty;
    public OrderSide Side { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Price { get; private set; }
    public decimal Total { get; private set; }
    public decimal Fee { get; private set; }
    public DateTime ExecutedAt { get; private set; }
    public string? ExchangeTradeId { get; private set; }

    private Trade() { } // EF Core

    public Trade(
        Guid id,
        Guid orderId,
        Guid userId,
        Guid assetId,
        string assetSymbol,
        OrderSide side,
        decimal quantity,
        decimal price,
        decimal total,
        decimal fee = 0,
        string? exchangeTradeId = null) : base(id)
    {
        OrderId = orderId;
        UserId = userId;
        AssetId = assetId;
        AssetSymbol = assetSymbol.ToUpper();
        Side = side;
        Quantity = quantity;
        Price = price;
        Total = total;
        Fee = fee;
        ExchangeTradeId = exchangeTradeId;
        ExecutedAt = DateTime.UtcNow;
    }
}
