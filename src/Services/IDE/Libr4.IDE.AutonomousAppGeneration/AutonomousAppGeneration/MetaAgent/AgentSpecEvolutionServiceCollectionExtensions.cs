using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.MetaAgent;

public static class AgentSpecEvolutionServiceCollectionExtensions
{
    public static IServiceCollection AddAgentSpecEvolution(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<AgentSpecEvolutionOptions>(
                configuration.GetSection(AgentSpecEvolutionOptions.SectionName));
            services.Configure<AgentSpecOptions>(options =>
            {
                var evolved = configuration[$"{AgentSpecEvolutionOptions.SectionName}:EvolvedSpecsRoot"];
                if (!string.IsNullOrWhiteSpace(evolved))
                    options.EvolvedSpecsDirectory = evolved;
            });
        }
        else
        {
            services.Configure<AgentSpecEvolutionOptions>(_ => { });
            services.Configure<AgentSpecOptions>(options =>
                options.EvolvedSpecsDirectory = ".libr4/agent-specs/evolved");
        }

        services.AddSingleton<IAgentSpecProposalStore, SqliteAgentSpecProposalStore>();
        services.AddSingleton<IAgentSpecVersionStore, FileAgentSpecVersionStore>();
        services.AddSingleton<IAgentSpecEvolutionService, AgentSpecEvolutionService>();
        services.AddSingleton<IAutonomousFinalizationHook, AgentSpecEvolutionFinalizationHook>();

        return services;
    }
}
