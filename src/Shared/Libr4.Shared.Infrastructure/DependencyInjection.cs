using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Infrastructure.Caching;
using Libr4.Shared.Infrastructure.Repositories;
using Libr4.Shared.Infrastructure.Events;
using Libr4.Shared.Kernel.Application;

namespace Libr4.Shared.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Event bus (domain events) — renamed to IDomainEventBus to avoid ambiguity
        services.AddSingleton<IDomainEventBus, DomainEventBus>();

        // Command/Query buses
        services.AddScoped<IQueryBus, QueryBus>();
        services.AddScoped<ICommandBus, CommandBus>();

        // Caching
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            options.InstanceName = "libr4:";
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Event publishing
        services.AddSingleton<IEventPublisher, EventPublisher>();
        services.AddHostedService<EventProcessingBackgroundService>();

        // Outbox pattern
        services.AddSingleton<IOutboxRepository, InMemoryOutboxRepository>();
        services.AddSingleton<IOutboxService, OutboxService>();

        return services;
    }
}