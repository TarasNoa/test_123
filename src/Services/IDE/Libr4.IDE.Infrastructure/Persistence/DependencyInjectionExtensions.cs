using Libr4.IDE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Infrastructure;

/// <summary>
/// Dependency injection extensions for IDE Infrastructure persistence.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds IDE persistence with PostgreSQL and registers EF Core repositories.
    /// </summary>
    public static IServiceCollection AddIdePersistence(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must be supplied.", nameof(connectionString));

        services.AddDbContextFactory<IdeDbContext>(opts =>
        {
            opts.UseNpgsql(connectionString);
            configure?.Invoke(opts);
        });

        // Register EF Core repositories as scoped (not singleton)
        services.AddScoped<IAgentEventRepository, EfAgentEventRepository>();
        services.AddScoped<IAgentOrchestrationRepository, EfAgentOrchestrationRepository>();
        services.AddScoped<IAppGenerationEntityRepository, AppGenerationRepository>();

        return services;
    }

    /// <summary>
    /// Generic variant for custom DbContext configuration (e.g., SQLite for tests).
    /// </summary>
    public static IServiceCollection AddIdePersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        services.AddDbContextFactory<IdeDbContext>(configure);

        services.AddScoped<IAgentEventRepository, EfAgentEventRepository>();
        services.AddScoped<IAgentOrchestrationRepository, EfAgentOrchestrationRepository>();
        services.AddScoped<IAppGenerationEntityRepository, AppGenerationRepository>();

        return services;
    }
}
