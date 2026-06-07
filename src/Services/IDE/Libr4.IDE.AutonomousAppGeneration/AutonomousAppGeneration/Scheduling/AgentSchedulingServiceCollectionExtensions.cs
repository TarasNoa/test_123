using Libr4.IDE.Application.AutonomousAppGeneration.Scheduling.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;

public static class AgentSchedulingServiceCollectionExtensions
{
    public static IServiceCollection AddAgentScheduling(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<AgentSchedulingOptions>(configuration.GetSection(AgentSchedulingOptions.SectionName));
        else
            services.Configure<AgentSchedulingOptions>(_ => { });

        services.AddSingleton<IScheduledAgentRunStore, SqliteScheduledAgentRunStore>();
        services.AddSingleton<IScheduledAgentRunService, ScheduledAgentRunService>();
        services.AddHostedService<ScheduledAgentRunHostedService>();
        services.AddHostedService<ScheduledAgentRunSchemaMigrator>();
        return services;
    }

    public static IServiceCollection AddAgentSchedulingMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.AddScoped<IScheduledAgentRunDispatcher, MassTransitScheduledAgentRunDispatcher>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ScheduledAgentRunConsumer>();
            configureConsumers?.Invoke(x);

            var envName = configuration["ASPNETCORE_ENVIRONMENT"]
                          ?? configuration["DOTNET_ENVIRONMENT"];
            var useInMemory = string.Equals(envName, "Testing", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase)
                              && !configuration.GetValue("AutonomousAppGeneration:AgentScheduling:RequireRabbitMq", false);

            if (useInMemory)
            {
                x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
                return;
            }

            x.UsingRabbitMq((ctx, cfg) =>
            {
                var host = configuration["RabbitMq:Host"] ?? "localhost";
                var user = configuration["RabbitMq:User"] ?? "guest";
                var password = configuration["RabbitMq:Password"] ?? "guest";
                cfg.Host(host, "/", h =>
                {
                    h.Username(user);
                    h.Password(password);
                });
                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
