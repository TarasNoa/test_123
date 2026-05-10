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
        // Core services temporarily disabled - require infrastructure dependencies
        // services.AddScoped<AgentOrchestrator>();
        // services.AddScoped<HierarchicalOrchestrationService>();
        // services.AddScoped<ISubagentDispatcher, SubagentDispatcher>();

        return services;
    }

    /// <summary>
    /// Add IDE Infrastructure services
    /// </summary>
    public static IServiceCollection AddIdeInfrastructure(this IServiceCollection services)
    {
        // Rust Sandbox client temporarily disabled
        // services.AddHttpClient<ISandboxClient, RustSandboxExecutor>();

        return services;
    }
}
