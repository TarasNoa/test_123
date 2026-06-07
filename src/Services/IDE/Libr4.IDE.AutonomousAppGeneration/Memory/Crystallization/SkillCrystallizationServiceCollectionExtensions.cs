using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Crystallization;

public static class SkillCrystallizationServiceCollectionExtensions
{
    public static IServiceCollection AddSkillCrystallization(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<SkillCrystallizationOptions>(
                configuration.GetSection("AutonomousAppGeneration:SkillCrystallization"));
        else
            services.Configure<SkillCrystallizationOptions>(_ => { });

        services.AddOptions<SkillActivationOptions>()
            .Configure<IOptions<SkillCrystallizationOptions>>((skill, crystallized) =>
            {
                if (!string.IsNullOrWhiteSpace(crystallized.Value.CrystallizedSkillsRoot))
                    skill.CrystallizedSkillsRoot = crystallized.Value.CrystallizedSkillsRoot;
            });

        services.AddSingleton<ISkillCrystallizer, FileSkillCrystallizer>();
        return services;
    }
}
