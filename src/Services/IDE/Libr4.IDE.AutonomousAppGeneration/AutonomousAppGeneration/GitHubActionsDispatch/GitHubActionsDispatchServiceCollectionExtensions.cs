using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public static class GitHubActionsDispatchServiceCollectionExtensions
{
    public static IServiceCollection AddGitHubActionsDispatch(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<GitHubActionsDispatchOptions>(configuration.GetSection(GitHubActionsDispatchOptions.SectionName));
        else
            services.Configure<GitHubActionsDispatchOptions>(_ => { });

        services.AddHttpClient<IGitHubApiClient, GitHubApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddSingleton<IGitHubShipService, GitHubShipService>();
        services.AddSingleton<IPullRequestService, PullRequestService>();
        services.AddSingleton<IGitHubCiWebhookService, GitHubCiWebhookService>();
        services.AddSingleton<IGitHubCiLogPrefetcher, GitHubCiLogPrefetcher>();
        services.AddSingleton<ICiRepairDispatcher, CiRepairDispatcher>();
        if (configuration is not null)
        {
            services.Configure<GitHubCiWebhookOptions>(configuration.GetSection(GitHubCiWebhookOptions.SectionName));
            services.Configure<CiRepairOptions>(configuration.GetSection(CiRepairOptions.SectionName));
        }
        else
        {
            services.Configure<GitHubCiWebhookOptions>(_ => { });
            services.Configure<CiRepairOptions>(_ => { });
        }
        return services;
    }
}
