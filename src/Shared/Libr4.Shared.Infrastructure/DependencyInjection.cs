using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Infrastructure.Caching;
using Libr4.Shared.Infrastructure.Repositories;
using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.Shared.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IEventBus, EventBus>();
        services.AddScoped<IQueryBus, QueryBus>();
        services.AddSingleton<ICommandBus, CommandBus>();

        // Caching
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            options.InstanceName = "libr4:";
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Event Publishing
        services.AddSingleton<IEventPublisher, EventPublisher>();
        services.AddHostedService<EventProcessingBackgroundService>();

        // Repositories
        services.AddSingleton<IUnitOfWork, UnitOfWork>();

        // Outbox Pattern
        services.AddSingleton<IOutboxService, OutboxService>();

        return services;
    }
}