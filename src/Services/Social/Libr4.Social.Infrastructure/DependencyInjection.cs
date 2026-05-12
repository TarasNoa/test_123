using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Libr4.Social.Domain.Network;
using Libr4.Social.Infrastructure.Repositories;

namespace Libr4.Social.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSocialInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<SocialDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "social"));
        });

        // Repositories
        services.AddScoped<ISocialNetworkRepository, SocialNetworkRepository>();

        // Event Handlers
        services.RegisterEventHandlers();

        return services;
    }

    private static void RegisterEventHandlers(this IServiceCollection services)
    {
        services.AddScoped<IEventHandler<PostCreatedEvent>, PostCreatedEventHandler>();
        services.AddScoped<IEventHandler<FollowerAddedEvent>, FollowerAddedEventHandler>();
    }
}