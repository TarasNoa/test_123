using MassTransit;
using Microsoft.Extensions.Logging;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Kernel.Application;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Application.Agents;
using Libr4.AI.Infrastructure.LLM;
using Libr4.AI.Infrastructure.Persistence;
using Libr4.AI.Infrastructure.Hooks;
using Libr4.AI.Infrastructure.Hooks.BuiltIn;
using Libr4.AI.Infrastructure.SandboxExecutor;
using Libr4.AI.Infrastructure.SessionRecovery;
using Libr4.AI.Infrastructure.Compounding;
using Libr4.AI.Infrastructure.Harness;
using Libr4.AI.Infrastructure.Exoskeleton;
using Libr4.AI.Infrastructure.Workbench;
using Libr4.AI.Infrastructure.Memory;
using Libr4.AI.Infrastructure.Profile;
using Libr4.AI.Infrastructure.Orchestration;
using Libr4.AI.Infrastructure.Subagent;
using Libr4.AI.Infrastructure.CodeGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Libr4.AI.Infrastructure.AI;
using Libr4.AI.Infrastructure.AI.Providers;

namespace Libr4.AI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAIInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<AIDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "ai"));
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ISessionStateManager, SessionStateManager>();
        services.AddScoped<IProjectKnowledgeBase, ProjectKnowledgeBase>();
        // services.AddScoped<IHarnessEnvironment, HarnessEnvironment>(); // TODO: Uncomment when HarnessEnvironment is available
        services.AddScoped<IExoskeletonProtocol, ExoskeletonProtocol>();
        services.AddScoped<IWorkbenchManager, WorkbenchManager>();
        services.AddScoped<IEnhancedMemoryWithGraph, EnhancedMemoryWithGraph>();
        services.AddScoped<IUserProfileExtractor, UserProfileExtractor>();
        services.AddScoped<IBackgroundAgentOrchestrator, BackgroundAgentOrchestrator>();
        services.AddScoped<IMultiProviderOrchestrator, MultiProviderOrchestrator>();
        services.AddScoped<IPersonaSystem, PersonaSystem>();
        services.AddScoped<IReactionEngine, ReactionEngine>();
        services.AddScoped<IGitIntegrationService, GitIntegrationService>();
        services.AddScoped<ICodebaseMapper, CodebaseMapper>();
        services.AddScoped<IAIDbContext>(sp => sp.GetRequiredService<AIDbContext>());

        // LLM Providers
        services.AddHttpClient<OllamaProvider>();
        services.AddHttpClient<LLM.OpenAIProvider>();
        services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();

        // AI Service Architecture (OpenRouter + multiple providers)
        services.AddHttpClient<AI.Providers.OpenRouterProvider>();
        services.AddHttpClient<AI.Providers.AlibabaCloudProvider>();
        services.AddHttpClient<AI.Providers.DockerModelRunnerProvider>();
        // Placeholder providers - temporarily disabled for testing
        // services.AddHttpClient<AI.Providers.OpenAIProvider>();
        services.AddHttpClient<AI.Providers.ClaudeProvider>();
        services.AddHttpClient<AI.Providers.GoogleProvider>();
        services.AddHttpClient<AI.Providers.DeepSeekProvider>();
        services.AddHttpClient<AI.Providers.GLMProvider>();
        services.AddSingleton<AIProviderFactory>();
        services.AddSingleton<LlmCircuitBreaker>();
        services.AddScoped<IAIService, AIService>();

        // Hooks
        services.AddScoped<HumanizerHook>();
        services.AddScoped<SessionRecoveryHook>();
        services.AddScoped<CompoundingHook>();
        services.AddScoped<HarnessHook>();
        services.AddScoped<ExoskeletonHook>();
        services.AddScoped<WorkbenchHook>();
        services.AddScoped<UserProfileHook>();
        
        // Hook Manager
        services.AddScoped<HookManager>();
        // services.AddSingleton<SessionLoggingHook>(); // TODO: Uncomment when SessionLogger is available
        services.AddSingleton<ToolUsageLoggingHook>();
        services.AddSingleton<ContextCompressionHook>();
        services.Configure<HumanizerOptions>(configuration.GetSection("Humanizer"));
        services.AddSingleton<HumanizerHook>();
        services.AddSingleton<GitIntegrationHook>();

        // Sandbox Executor
        services.Configure<SandboxExecutorOptions>(configuration.GetSection("SandboxExecutor"));
        services.AddSingleton<SandboxExecutorService>();

        // Code Graph
        // services.AddSingleton<CodeGraphService>(); // TODO: Uncomment when CodeGraphService is available

        // LLM Router
        services.AddSingleton<LLMRouter>();

        // MassTransit with RabbitMQ
        services.AddLibr4MassTransit(configuration, x =>
        {
            x.AddConsumers(typeof(DependencyInjection).Assembly);
        });

        // Health checks
        services.AddHealthChecks()
            .AddDbContextCheck<AIDbContext>("ai-db");

        return services;
    }
}
