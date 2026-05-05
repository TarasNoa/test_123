using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Trading.Domain.Orders.Events;

public sealed record OrderCreated(
    Guid OrderId,
    Guid UserId,
    string AssetSymbol,
    string OrderType,
    string Side,
    decimal Quantity,
    decimal? Price,
    DateTime CreatedAt)  : DomainEvent;

public sealed record OrderSubmitted(
    Guid OrderId,
    Guid UserId,
    string AssetSymbol,
    DateTime SubmittedAt)  : DomainEvent;

public sealed record OrderFilled(
    Guid OrderId,
    Guid UserId,
    string AssetSymbol,
    Guid TradeId,
    decimal FilledQuantity,
    decimal FillPrice,
    decimal Total,
    bool IsComplete,
    DateTime FilledAt)  : DomainEvent;

public sealed record OrderCancelled(
    Guid OrderId,
    Guid UserId,
    string Reason,
    DateTime CancelledAt)  : DomainEvent;

public sealed record OrderRejected(
    Guid OrderId,
    Guid UserId,
    string Reason,
    DateTime RejectedAt)  : DomainEvent;
