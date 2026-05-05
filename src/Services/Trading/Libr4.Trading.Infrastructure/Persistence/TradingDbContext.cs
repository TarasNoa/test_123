using Libr4.Trading.Application.Abstractions;
using Libr4.Trading.Domain.Assets;
using Libr4.Trading.Domain.Orders;
using Libr4.Trading.Domain.Portfolios;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Trading.Infrastructure.Persistence;

public class TradingDbContext : DbContext, ITradingDbContext
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetPrice> AssetPrices => Set<AssetPrice>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<PortfolioPosition> PortfolioPositions => Set<PortfolioPosition>();

    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("trading");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
