using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.InlineCompletion;

public static class InlineCompletionServiceCollectionExtensions
{
    public static IServiceCollection AddInlineCompletion(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<InlineCompletionOptions>(configuration.GetSection(InlineCompletionOptions.SectionName));
        else
            services.Configure<InlineCompletionOptions>(_ => { });

        services.AddScoped<IInlineCompletionService, InlineCompletionService>();
        return services;
    }
}
