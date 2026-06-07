using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.HostProfiles;

public static class AutonomousHostProfileServiceCollectionExtensions
{
    public static IServiceCollection AddAutonomousHostProfiles(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<AutonomousHostProfileOptions>(
                configuration.GetSection(AutonomousHostProfileOptions.SectionName));
        else
            services.Configure<AutonomousHostProfileOptions>(_ => { });

        services.AddSingleton<IAutonomousHostProfileService, AutonomousHostProfileService>();
        services.AddHostedService<AutonomousHostProfileStartupLogger>();
        return services;
    }

    public static void AddAutonomousHostProfileConfiguration(this IConfigurationBuilder configurationBuilder)
    {
        var interim = configurationBuilder.Build();
        var profileName = interim[$"{AutonomousHostProfileOptions.SectionName}:ActiveProfile"]
                          ?? Environment.GetEnvironmentVariable("LIBR4_HOST_PROFILE");

        if (string.IsNullOrWhiteSpace(profileName))
            return;

        configurationBuilder.AddJsonFile(
            $"appsettings.Profile.{profileName}.json",
            optional: true,
            reloadOnChange: true);
    }
}
