using Libr4.Shared.Kernel.Application;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.Shared.Infrastructure.Messaging;

public static class MassTransitExtensions
{
    public static IServiceCollection AddLibr4MassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        services.AddScoped<IEventBus, MassTransitEventBus>();

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            configure?.Invoke(x);

            x.UsingRabbitMq((ctx, cfg) =>
            {
                var host = configuration["RabbitMq:Host"] ?? "localhost";
                var user = configuration["RabbitMq:User"] ?? "guest";
                var password = configuration["RabbitMq:Password"] ?? throw new InvalidOperationException("RabbitMq:Password not configured");

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
