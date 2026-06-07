using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Extensions;

public static class ExtensionServiceCollectionExtensions
{
    public static IServiceCollection AddExtensionHost(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<ExtensionHostOptions>(configuration.GetSection(ExtensionHostOptions.SectionName));
        else
            services.Configure<ExtensionHostOptions>(_ => { });

        services.AddSingleton<ExtensionHost>();
        services.AddSingleton<IExtensionHost>(sp => sp.GetRequiredService<ExtensionHost>());
        services.AddSingleton<ISandboxedExtensionRunner, SandboxedExtensionRunner>();
        services.AddSingleton<ExtensionLifecycleHookBridge>();

        foreach (var kind in Enum.GetValues<AgentHookKind>())
        {
            services.AddSingleton<IAgentLifecycleHook>(sp =>
                new ExtensionLifecycleHookDispatcher(sp.GetRequiredService<ExtensionLifecycleHookBridge>(), kind));
        }

        services.AddHostedService<ExtensionHostStartup>();

        services.RemoveAll<ISkillManifestRegistry>();
        services.AddSingleton<ISkillManifestRegistry>(sp =>
            new ExtensionAwareSkillManifestRegistry(
                new FileSkillManifestRegistry(sp.GetRequiredService<IOptions<SkillActivationOptions>>()),
                sp.GetRequiredService<IExtensionHost>()));

        services.RemoveAll<IAgentToolRegistry>();
        services.AddSingleton<IAgentToolRegistry>(sp =>
            new ExtensionAwareAgentToolRegistry(
                sp.GetServices<IAgentTool>(),
                sp.GetRequiredService<IExtensionHost>(),
                sp.GetRequiredService<ISandboxedExtensionRunner>()));

        return services;
    }
}
