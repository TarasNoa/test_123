using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.Shared.Web.Persistence;

/// <summary>
/// Applies migrations or EnsureCreated without crashing the host when the database is unavailable (local dev / tests).
/// </summary>
public static class DatabaseBootstrapExtensions
{
    public static async Task ApplyDatabaseBootstrapAsync<TContext>(
        this IHost host,
        bool useMigrations,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        try
        {
            if (useMigrations)
                await db.Database.MigrateAsync(cancellationToken);
            else
                await db.Database.EnsureCreatedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Database bootstrap skipped for {Context}: {Message}",
                typeof(TContext).Name,
                ex.Message);
        }
    }
}
