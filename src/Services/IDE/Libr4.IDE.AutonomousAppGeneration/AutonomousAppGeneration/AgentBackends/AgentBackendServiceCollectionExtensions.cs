using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

public static class AgentBackendServiceCollectionExtensions
{
    public static IServiceCollection AddAgentBackends(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<ExternalAgentBackendOptions>(configuration.GetSection(ExternalAgentBackendOptions.SectionName));
        else
            services.Configure<ExternalAgentBackendOptions>(_ => { });

        services.AddSingleton<IsolatedExternalBackendRunner>(sp =>
        {
            var runtime = sp.GetService<IIsolatedRuntime>()
                          ?? new ProcessIsolatedRuntime(sp.GetRequiredService<ILogger<ProcessIsolatedRuntime>>());
            return new IsolatedExternalBackendRunner(
                runtime,
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalAgentBackendOptions>>().Value,
                sp.GetRequiredService<ILogger<IsolatedExternalBackendRunner>>());
        });

        services.AddSingleton<Libr4NativeAgentBackend>();
        services.AddSingleton<CodexCliAgentBackend>();
        services.AddSingleton<OpenCodeCliAgentBackend>();
        services.AddSingleton<CursorSdkAgentBackend>();
        services.AddSingleton<ExternalAcpAgentBackend>();
        services.AddSingleton<IAgentBackend>(sp => sp.GetRequiredService<Libr4NativeAgentBackend>());
        services.AddSingleton<IAgentBackend>(sp => sp.GetRequiredService<CodexCliAgentBackend>());
        services.AddSingleton<IAgentBackend>(sp => sp.GetRequiredService<OpenCodeCliAgentBackend>());
        services.AddSingleton<IAgentBackend>(sp => sp.GetRequiredService<CursorSdkAgentBackend>());
        services.AddSingleton<IAgentBackend>(sp => sp.GetRequiredService<ExternalAcpAgentBackend>());
        services.AddSingleton<IAgentBackendRegistry, AgentBackendRegistry>();
        services.AddSingleton<IAgentBackendCoordinator, AgentBackendCoordinator>();
        return services;
    }
}
