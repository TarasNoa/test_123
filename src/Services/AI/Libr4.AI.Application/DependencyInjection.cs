using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Application.Agents;
using Libr4.AI.Application.CVAnalysis;
using Libr4.AI.Application.DocumentVerification;

namespace Libr4.AI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAIApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IOrderAssistantService, OrderAssistantService>();
        services.AddScoped<ITaskRecommendationService, TaskRecommendationService>();
        services.AddScoped<ICVAnalysisService, CVAnalysisService>();
        services.AddScoped<IDocumentVerificationService, DocumentVerificationService>();

        return services;
    }
}
