using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Application.Agents;
using Libr4.AI.Application.CVAnalysis;

namespace Libr4.AI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAIApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // services.AddSingleton<IAgentService, AgentService>(); // depends on scoped DbContextOptions
        // services.AddSingleton<IOrderAssistantService, OrderAssistantService>();
        // services.AddSingleton<ITaskRecommendationService, TaskRecommendationService>();
        services.AddSingleton<ICVAnalysisService, CVAnalysisService>();

        return services;
    }
}
