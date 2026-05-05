using Libr4.Trading.Domain.Assets;
using Libr4.Trading.Domain.Orders;

namespace Libr4.Trading.Application.Dtos;

public record AssetDto(
    Guid Id,
    string Symbol,
    string Name,
    AssetType Type,
    string? Exchange,
    int Precision,
    bool IsActive);

public record AssetPriceDto(
    Guid AssetId,
    string Symbol,
    decimal Price,
    decimal? Bid,
    decimal? Ask,
    decimal? Volume24h,
    decimal? Change24h,
    DateTime Timestamp);

public record OrderDto(
    Guid Id,
    Guid AssetId,
    string AssetSymbol,
    OrderType Type,
    OrderSide Side,
    OrderStatus Status,
    decimal Quantity,
    decimal? Price,
    decimal? StopPrice,
    decimal FilledQuantity,
    decimal? AverageFillPrice,
    DateTime CreatedAt,
    DateTime? ExecutedAt);

public record TradeDto(
    Guid Id,
    Guid OrderId,
    string AssetSymbol,
    OrderSide Side,
    decimal Quantity,
    decimal Price,
    decimal Total,
    decimal Fee,
    DateTime ExecutedAt);

public record PortfolioDto(
    Guid Id,
    string Name,
    bool IsDefault,
    DateTime CreatedAt,
    List<PortfolioPositionDto> Positions);

public record PortfolioPositionDto(
    Guid AssetId,
    string AssetSymbol,
    decimal Quantity,
    decimal AverageEntryPrice,
    decimal? CurrentPrice,
    decimal? MarketValue,
    decimal? UnrealizedPnl);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
