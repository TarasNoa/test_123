using Libr4.Collaboration.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.Collaboration.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCollaborationApplication(this IServiceCollection services)
    {
        services.AddScoped<ICollaborationService, CollaborationService>();
        return services;
    }
}
