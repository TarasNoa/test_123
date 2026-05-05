using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Libr4.Shared.Infrastructure.Caching;

public static class RedisExtensions
{
    public static IServiceCollection AddLibr4Redis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
        services.AddStackExchangeRedisCache(o => o.Configuration = connectionString);

        return services;
    }
}
