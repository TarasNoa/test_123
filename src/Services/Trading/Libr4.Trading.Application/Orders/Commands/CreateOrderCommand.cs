using FluentValidation;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Contracts.IntegrationEvents.Trading;
using Libr4.Shared.Kernel.Application;
using Libr4.Trading.Application.Abstractions;
using Libr4.Trading.Domain;
using Libr4.Trading.Domain.Assets;
using Libr4.Trading.Domain.Orders;
using Libr4.Trading.Domain.Portfolios;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Trading.Application.Orders.Commands;

public record CreateOrderCommand(
    Guid AssetId,
    OrderType Type,
    OrderSide Side,
    decimal Quantity,
    decimal? Price = null,
    decimal? StopPrice = null,
    TimeInForce TimeInForce = TimeInForce.GTC,
    DateTime? ExpiresAt = null) : IRequest<Result<Guid>>;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .When(x => x.Type == OrderType.Limit);
        RuleFor(x => x.StopPrice)
            .GreaterThan(0)
            .When(x => x.Type == OrderType.StopLoss || x.Type == OrderType.TakeProfit);
    }
}

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly ITradingDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;
    private readonly IMarketDataService _marketData;

    public CreateOrderHandler(
        ITradingDbContext context,
        ICurrentUser currentUser,
        IEventBus eventBus,
        IMarketDataService marketData)
    {
        _context = context;
        _currentUser = currentUser;
        _eventBus = eventBus;
        _marketData = marketData;
    }

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var asset = await _context.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken);

        if (asset == null)
            return Result.Failure<Guid>(TradingErrors.AssetNotFound);

        if (!asset.IsActive)
            return Result.Failure<Guid>(TradingErrors.TradingDisabled);

        // For market orders, get current price for validation
        decimal? executionPrice = null;
        if (request.Type == OrderType.Market)
        {
            var priceData = await _marketData.GetPriceAsync(asset.Symbol, cancellationToken);
            if (priceData == null)
                return Result.Failure<Guid>(TradingErrors.MarketDataNotAvailable);
            executionPrice = priceData.Price;
        }

        var order = new Order(
            Guid.NewGuid(),
            userId,
            asset.Id,
            asset.Symbol,
            request.Type,
            request.Side,
            request.Quantity,
            request.Price,
            request.StopPrice,
            request.TimeInForce,
            request.ExpiresAt);

        // For paper trading - auto-fill market orders
        if (request.Type == OrderType.Market && executionPrice.HasValue)
        {
            order.Submit();
            order.Fill(request.Quantity, executionPrice.Value);

            // Update portfolio position
            await UpdatePortfolioPosition(userId, asset, request.Side, request.Quantity, executionPrice.Value, cancellationToken);
        }
        else
        {
            order.Submit();
        }

        await _context.Orders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventBus.PublishAsync(
            new OrderCreatedIntegrationEvent(
                order.Id,
                userId,
                asset.Symbol,
                order.Type.ToString(),
                order.Side.ToString(),
                order.Quantity,
                order.Price,
                DateTimeOffset.UtcNow),
            cancellationToken);

        if (order.Status == OrderStatus.Filled)
        {
            var trade = order.Trades.First();
            await _eventBus.PublishAsync(
                new OrderFilledIntegrationEvent(
                    order.Id,
                    userId,
                    asset.Symbol,
                    trade.Id,
                    trade.Quantity,
                    trade.Price,
                    trade.Total,
                    true,
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }

        return Result.Success(order.Id);
    }

    private async Task UpdatePortfolioPosition(
        Guid userId,
        Asset asset,
        OrderSide side,
        decimal quantity,
        decimal price,
        CancellationToken cancellationToken)
    {
        var portfolio = await _context.Portfolios
            .Include(p => p.Positions)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsDefault, cancellationToken);

        if (portfolio == null)
        {
            portfolio = new Portfolio(Guid.NewGuid(), userId, "Default", true);
            await _context.Portfolios.AddAsync(portfolio, cancellationToken);
        }

        var currentPosition = portfolio.GetPositionQuantity(asset.Id) ?? 0;
        var newQuantity = side == OrderSide.Buy
            ? currentPosition + quantity
            : currentPosition - quantity;

        // Calculate new average price (simplified)
        var avgPrice = side == OrderSide.Buy && newQuantity > 0
            ? ((currentPosition * (portfolio.Positions.FirstOrDefault(p => p.AssetId == asset.Id)?.AverageEntryPrice ?? 0)) + (quantity * price)) / newQuantity
            : (portfolio.Positions.FirstOrDefault(p => p.AssetId == asset.Id)?.AverageEntryPrice ?? price);

        portfolio.UpdatePosition(asset.Id, asset.Symbol, newQuantity, avgPrice);
    }
}
