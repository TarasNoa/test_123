namespace Libr4.Shared.Contracts.IntegrationEvents.Trading;

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    string AssetSymbol,
    string OrderType,
    string Side,
    decimal Quantity,
    decimal? Price,
    DateTimeOffset OccurredOn);

public sealed record OrderFilledIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    string AssetSymbol,
    Guid TradeId,
    decimal FilledQuantity,
    decimal FillPrice,
    decimal Total,
    bool IsComplete,
    DateTimeOffset OccurredOn);

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    string Reason,
    DateTimeOffset OccurredOn);
