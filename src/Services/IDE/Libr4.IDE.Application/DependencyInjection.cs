using Libr4.IDE.Application.Services;
using Libr4.IDE.Infrastructure.Persistence;
using Libr4.IDE.Infrastructure.Sandbox;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application;

/// <summary>
/// Dependency injection extensions for IDE Application layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add IDE Application services with Golden Stack pattern
    /// </summary>
    public static IServiceCollection AddIdeApplication(this IServiceCollection services)
    {
        // Register orchestrator (thin bridge between F# and Rust)
        services.AddScoped<AgentOrchestrator>();

        return services;
    }

    /// <summary>
    /// Add IDE Infrastructure services
    /// </summary>
    public static IServiceCollection AddIdeInfrastructure(this IServiceCollection services)
    {
        // Register Rust Sandbox client
        services.AddHttpClient<ISandboxClient, RustSandboxExecutor>();

        return services;
    }
}
