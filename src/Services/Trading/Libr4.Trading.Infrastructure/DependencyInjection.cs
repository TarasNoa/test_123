using Libr4.Trading.Application.Abstractions;
using Libr4.Trading.Infrastructure.MarketData;
using Libr4.Trading.Infrastructure.Persistence;
using Libr4.Shared.Infrastructure.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.Trading.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<TradingDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "trading"));
        });

        services.AddScoped<ITradingDbContext>(sp => sp.GetRequiredService<TradingDbContext>());

        // HTTP Client for market data
        services.AddHttpClient<IMarketDataService, BinanceMarketDataService>();

        // MassTransit with RabbitMQ
        services.AddLibr4MassTransit(configuration, x =>
        {
            x.AddConsumers(typeof(DependencyInjection).Assembly);
        });

        // Health checks
        services.AddHealthChecks()
            .AddDbContextCheck<TradingDbContext>("trading-db");

        return services;
    }
}
