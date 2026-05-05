using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Persistence;

/// <summary>
/// P2-1 of audit roadmap. Composition extensions for persistent storage.
///
/// Usage from host:
/// <code>
///   services.AddAutonomousAppGeneration();              // base in-memory wiring
///   services.AddPostgresPersistence(connectionString);  // overrides repository to hybrid EF Core
/// </code>
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>Registers <see cref="EfCoreAppGenerationRepository"/> backed by Postgres.</summary>
    public static IServiceCollection AddPostgresPersistence(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must be supplied.", nameof(connectionString));

        services.AddDbContextFactory<AutoGenDbContext>(opts =>
        {
            opts.UseNpgsql(connectionString);
            configure?.Invoke(opts);
        });

        // Replace the default IAppGenerationRepository with the hybrid wrapper.
        services.RemoveAll<IAppGenerationRepository>();
        services.AddSingleton<IAppGenerationRepository>(sp =>
        {
            var dbFactory = sp.GetRequiredService<IDbContextFactory<AutoGenDbContext>>();
            // var inMem = new InMemoryAppGenerationRepository(); // TODO: Uncomment when available
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EfCoreAppGenerationRepository>>();
            return new EfCoreAppGenerationRepository(dbFactory, null, logger);
        });

        return services;
    }

    /// <summary>
    /// Generic variant taking a pre-configured options action (e.g. SQLite for tests, Cosmos, etc.).
    /// </summary>
    public static IServiceCollection AddAutoGenPersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        services.AddDbContextFactory<AutoGenDbContext>(configure);

        services.RemoveAll<IAppGenerationRepository>();
        services.AddSingleton<IAppGenerationRepository>(sp =>
        {
            var dbFactory = sp.GetRequiredService<IDbContextFactory<AutoGenDbContext>>();
            // var inMem = new InMemoryAppGenerationRepository(); // TODO: Uncomment when available
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EfCoreAppGenerationRepository>>();
            return new EfCoreAppGenerationRepository(dbFactory, null, logger);
        });

        return services;
    }
}
