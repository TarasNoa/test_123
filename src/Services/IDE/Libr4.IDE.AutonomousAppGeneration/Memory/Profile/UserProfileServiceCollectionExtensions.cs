using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;

public static class UserProfileServiceCollectionExtensions
{
    public static IServiceCollection AddUserProfiles(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<UserProfileOptions>(
                configuration.GetSection("AutonomousAppGeneration:UserProfile"));
        else
            services.Configure<UserProfileOptions>(_ => { });

        services.AddSingleton<IUserProfileStore, FileUserProfileStore>();
        services.AddSingleton<IUserProfileService, UserProfileService>();
        services.AddSingleton<IAutonomousFinalizationHook, UserProfileFinalizationHook>();
        return services;
    }
}
