using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public static class VerifyServiceCollectionExtensions
{
    public static IServiceCollection AddVerifySubagent(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<VerifySubagentOptions>(configuration.GetSection(VerifySubagentOptions.SectionName));
        else
            services.Configure<VerifySubagentOptions>(_ => { });

        services.AddSingleton<IVerifyFailureContextStore, VerifyFailureContextStore>();
        services.AddSingleton<IVerifyEvidenceStore, FileSystemVerifyEvidenceStore>();
        services.AddSingleton<IReadOnlyDictionary<string, VerifyRecipe>>(_ =>
            VerifyRecipeCatalog.BuildAll().ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase));
        services.AddScoped<IVerifyRecipeLlmDetector, VerifyRecipeLlmDetector>();
        services.AddScoped<IVerifyRecipeRegistry, VerifyRecipeRegistry>();
        services.AddScoped<IVerifyReadinessProbe, VerifyReadinessProbe>();
        services.AddScoped<IObscuraVerifySmokeRunner, ObscuraVerifySmokeRunner>();
        services.AddScoped<IVerifyGateService, VerifyGateService>();
        services.AddScoped<IVerifyOrchestrator, VerifyOrchestrator>();
        services.AddScoped<IVerifySubagentService, VerifySubagentService>();
        return services;
    }
}
