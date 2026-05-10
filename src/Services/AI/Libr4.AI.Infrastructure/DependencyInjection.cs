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
using Libr4.AI.Infrastructure.ML;
using Libr4.AI.Domain.Agents;
using Libr4.AI.Infrastructure.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Libr4.AI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAIInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AIDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AIDatabase")));

        // Repositories
        services.AddScoped<IAgentRepository, AgentRepository>();

        // Caching
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        // Rust Inference Bridge
        services.AddSingleton<IRustInferenceBridge, RustMLInferenceBridge>();

        // Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<AIDbContext>("AIDatabase")
            .AddRedis(configuration.GetConnectionString("Redis"), "RedisCache");

        // Metrics (using Prometheus)
        services.AddOpenTelemetry()
            .WithMetrics(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

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

    public static IServiceCollection AddAIAgentInfrastructure(this IServiceCollection services)
    {
        // Router
        services.AddSingleton<IAgentRouter, AgentRouter>();

        // Register agents
        services.AddSingleton<OrchestratorAgent>(sp =>
        {
            var router = sp.GetRequiredService<IAgentRouter>();
            var logger = sp.GetRequiredService<ILogger<BaseAgent>>();
            return new OrchestratorAgent(logger, router);
        });

        services.AddSingleton<CodeWriterAgent>(sp =>
        {
            var generator = sp.GetRequiredService<ICodeGenerationService>();
            var logger = sp.GetRequiredService<ILogger<BaseAgent>>();
            return new CodeWriterAgent(logger, generator);
        });

        services.AddSingleton<CodeReviewerAgent>(sp =>
        {
            var analyzer = sp.GetRequiredService<ICodeAnalysisService>();
            var logger = sp.GetRequiredService<ILogger<BaseAgent>>();
            return new CodeReviewerAgent(logger, analyzer);
        });

        services.AddSingleton<DebuggerAgent>(sp =>
        {
            var executor = sp.GetRequiredService<ICodeExecutor>();
            var errorAnalyzer = sp.GetRequiredService<ICodeErrorAnalyzer>();
            var repairService = sp.GetRequiredService<ICodeRepairService>();
            var logger = sp.GetRequiredService<ILogger<BaseAgent>>();
            return new DebuggerAgent(logger, executor, errorAnalyzer, repairService);
        });

        // Agent initialization
        services.AddSingleton<IAgentFactory, AgentFactory>();

        return services;
    }
}
