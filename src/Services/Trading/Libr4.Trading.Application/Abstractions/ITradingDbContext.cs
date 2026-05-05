using Libr4.Trading.Domain.Assets;
using Libr4.Trading.Domain.Orders;
using Libr4.Trading.Domain.Portfolios;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Trading.Application.Abstractions;

public interface ITradingDbContext
{
    DbSet<Asset> Assets { get; }
    DbSet<AssetPrice> AssetPrices { get; }
    DbSet<Order> Orders { get; }
    DbSet<Trade> Trades { get; }
    DbSet<Portfolio> Portfolios { get; }
    DbSet<PortfolioPosition> PortfolioPositions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
