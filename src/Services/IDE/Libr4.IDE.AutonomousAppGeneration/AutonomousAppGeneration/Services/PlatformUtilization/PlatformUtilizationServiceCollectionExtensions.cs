using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

public static class PlatformUtilizationServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformUtilization(this IServiceCollection services)
    {
        services.AddSingleton<IPlatformCapabilityBriefingService, PlatformCapabilityBriefingService>();
        services.AddScoped<IPlatformRunBootstrapService, PlatformRunBootstrapService>();
        services.AddSingleton<IPlatformJitJsonlAudit, PlatformJitJsonlAudit>();
        services.AddSingleton<IPlatformJitCapabilityService, PlatformJitCapabilityService>();
        return services;
    }
}
