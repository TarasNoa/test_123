using Microsoft.Extensions.DependencyInjection;
using Libr4.Analytics.Application.Abstractions;

namespace Libr4.Analytics.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsApplication(this IServiceCollection services)
    {
        services.AddScoped<IMetricsService, MetricsService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
