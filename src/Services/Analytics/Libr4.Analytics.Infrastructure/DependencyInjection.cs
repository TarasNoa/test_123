using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Libr4.Analytics.Application.Abstractions;
using Libr4.Analytics.Infrastructure.Repositories;

namespace Libr4.Analytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AnalyticsDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "analytics"));
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IMetricRepository, MetricRepository>();

        return services;
    }
}
