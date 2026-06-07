using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public static class HermesMemoryServiceCollectionExtensions
{
    public static IServiceCollection AddHermesMemory(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<HermesMemoryOptions>(
                configuration.GetSection("AutonomousAppGeneration:HermesMemory"));
            services.Configure<HermesMemoryManagerOptions>(
                configuration.GetSection("AutonomousAppGeneration:HermesMemory:Manager"));
            services.Configure<MemoryToolOptions>(
                configuration.GetSection("AutonomousAppGeneration:HermesMemory:Tools"));
        }
        else
        {
            services.Configure<HermesMemoryOptions>(_ => { });
            services.Configure<HermesMemoryManagerOptions>(_ => { });
            services.Configure<MemoryToolOptions>(_ => { });
        }

        services.AddSingleton<SqliteHermesMemoryStore>();
        services.AddSingleton<IHermesMemoryManager, HermesMemoryManager>();
        services.AddSingleton<IAgentLifecycleHook, HermesMemoryLifecycleHook>();
        services.AddHostedService<HermesMemorySchemaMigrator>();
        return services;
    }
}
