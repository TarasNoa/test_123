using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using Libr4.Trading.Application.Abstractions;
using Libr4.Trading.Application.Dtos;
using Libr4.Trading.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Trading.Application.Portfolios.Queries;

public record GetMyPortfolioQuery : IRequest<Result<PortfolioDto>>;

public class GetMyPortfolioHandler : IRequestHandler<GetMyPortfolioQuery, Result<PortfolioDto>>
{
    private readonly ITradingDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IMarketDataService _marketData;

    public GetMyPortfolioHandler(
        ITradingDbContext context,
        ICurrentUser currentUser,
        IMarketDataService marketData)
    {
        _context = context;
        _currentUser = currentUser;
        _marketData = marketData;
    }

    public async Task<Result<PortfolioDto>> Handle(GetMyPortfolioQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var portfolio = await _context.Portfolios
            .AsNoTracking()
            .Include(p => p.Positions)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsDefault, cancellationToken);

        if (portfolio == null)
        {
            // Return empty portfolio
            return Result.Success(new PortfolioDto(
                Guid.Empty,
                "Default",
                true,
                DateTime.UtcNow,
                new List<PortfolioPositionDto>()));
        }

        // Get current prices for all positions
        var symbols = portfolio.Positions.Select(p => p.AssetSymbol).ToList();
        var prices = await _marketData.GetPricesAsync(symbols, cancellationToken);
        var priceDict = prices.ToDictionary(p => p.Symbol, p => p.Price);

        var positions = portfolio.Positions.Select(pos =>
        {
            var currentPrice = priceDict.TryGetValue(pos.AssetSymbol, out var price) ? price : (decimal?)null;
            var marketValue = currentPrice.HasValue ? pos.Quantity * currentPrice.Value : (decimal?)null;
            var unrealizedPnl = currentPrice.HasValue && pos.Quantity > 0
                ? pos.Quantity * (currentPrice.Value - pos.AverageEntryPrice)
                : (decimal?)null;

            return new PortfolioPositionDto(
                pos.AssetId,
                pos.AssetSymbol,
                pos.Quantity,
                pos.AverageEntryPrice,
                currentPrice,
                marketValue,
                unrealizedPnl);
        }).ToList();

        return Result.Success(new PortfolioDto(
            portfolio.Id,
            portfolio.Name,
            portfolio.IsDefault,
            portfolio.CreatedAt,
            positions));
    }
}
