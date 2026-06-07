using Libr4.AI.Application.Abstractions;
using Libr4.AI.Infrastructure.AI;
using Libr4.AI.Infrastructure.AI.Providers;
using LlmCircuitBreakerOptions = Libr4.AI.Infrastructure.AI.LlmCircuitBreakerOptions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;
using Libr4.IDE.Application.AutonomousAppGeneration.HostProfiles;
using Libr4.IDE.Application.AutonomousAppGeneration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;
using Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Application.AutonomousAppGeneration.Commands;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.AutonomousAppGeneration.Host.Endpoints;
using HostEndpoints = Libr4.IDE.AutonomousAppGeneration.Host.Endpoints.AutonomousAppGenerationHostEndpoints;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((_, config) =>
    config.AddAutonomousHostProfileConfiguration());

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

try
{
    Log.Information("Starting AutonomousAppGeneration Host");
    Log.Information(
        "Config env={Env} AI:DefaultProvider={AiProvider} ProviderCapabilityMatrix:DefaultProvider={MatrixProvider}",
        builder.Environment.EnvironmentName,
        builder.Configuration["AI:DefaultProvider"],
        builder.Configuration["ProviderCapabilityMatrix:DefaultProvider"]);
    builder.Host.UseSerilog();

    // ---- Core ASP.NET ----------------------------------------------------------
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    // ---- AI provider (OpenRouter + Alibaba Cloud + local Docker Model Runner + Ollama) --
    // Lightweight wiring: we only need AIService + providers, not the full
    // AI microservice infrastructure (no DB, no RabbitMQ). Configure API keys
    // (or local endpoint) via appsettings.json.
    builder.Services.Configure<GpuThrottleOptions>(
        builder.Configuration.GetSection(GpuThrottleOptions.SectionName));
    builder.Services.AddSingleton<IGpuResourceGuard, NvidiaGpuResourceGuard>();
    builder.Services.AddHttpClient<OpenRouterProvider>()
        .ConfigurePrimaryHttpMessageHandler(OpenRouterHttpClientHandlerFactory.Create)
        .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(15));
    builder.Services.AddSingleton<AlibabaCloudProvider>();
    builder.Services.AddSingleton<DockerModelRunnerProvider>();
    builder.Services.AddSingleton<OllamaProvider>();
    builder.Services.AddSingleton<AIProviderFactory>();
    // P1-5: circuit breaker must be registered before AIService (it's a constructor dependency).
    builder.Services.AddSingleton(_ => new LlmCircuitBreakerOptions
    {
        FailureThreshold = 15,
        OpenDuration = TimeSpan.FromMinutes(2)
    });
    builder.Services.AddSingleton<LlmCircuitBreaker>();
    builder.Services.AddScoped<Libr4.AI.Infrastructure.Hooks.HookManager>();
    builder.Services.AddSingleton<Libr4.AI.Infrastructure.LLMRouter>();
    builder.Services.AddScoped<IAIService, AIService>();

    // ---- MediatR + orchestrator feature ---------------------------------------
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(StartAppGenerationCommand).Assembly));
    var runtimeProvider = builder.Configuration["AutonomousAppGeneration:RuntimeProvider"];
    var allowProcessFallback =
        builder.Configuration.GetValue("AutonomousAppGeneration:AllowProcessFallback", true);
    builder.Services.Configure<RuntimePolicyOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:RuntimePolicy"));
    builder.Services.Configure<AutonomousLoopGuardOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:LoopGuard"));
    builder.Services.Configure<AutonomousRetryOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:RetryPolicy"));
    builder.Services.Configure<AutonomousGenerationOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:Generation"));
    builder.Services.Configure<AutonomousQualityGateOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:QualityGates"));
    builder.Services.Configure<AutonomousBenchmarkModeOptions>(
        builder.Configuration.GetSection(AutonomousBenchmarkModeOptions.SectionName));
    builder.Services.Configure<BenchmarkExportOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:BenchmarkExport"));
    builder.Services.Configure<DiagnosticsExportOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:DiagnosticsExport"));
    builder.Services.Configure<McpExecutionOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:AgentIntegration:Mcp"));
    builder.Services.Configure<McpExecutionPolicyOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:AgentIntegration:McpPolicy"));
    builder.Services.Configure<SecurityReviewGateOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:AgentIntegration:SecurityReview"));
    builder.Services.Configure<ContextPackOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:AgentIntegration:ContextPack"));
    builder.Services.Configure<CascadePlannerOptions>(
        builder.Configuration.GetSection("AutonomousAppGeneration:AgentIntegration:CascadePlanner"));
    builder.Services.Configure<Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.ProviderMatrixOptions>(
        builder.Configuration.GetSection(Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.ProviderMatrixOptions.SectionName));
    builder.Services.AddAutonomousAppGeneration(
        runtimeProvider,
        allowProcessFallback,
        builder.Configuration,
        registerObscuraMediatRHandlers: false);
    if (builder.Configuration.GetValue("AutonomousAppGeneration:AgentScheduling:UseMassTransit", false))
        builder.Services.AddAgentSchedulingMassTransit(
            builder.Configuration,
            cfg => cfg.AddRunHandoffMassTransitConsumers());
    // Obscura browser plane (idempotent; also registered inside AddAutonomousAppGeneration).
    builder.Services.AddObscuraBrowserPlane(builder.Configuration, registerMediatRHandlers: false);
    builder.Services.AddObscuraSessionHostedServices();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapGet("/", () => Results.Redirect("/swagger"));
    HostEndpoints.MapAutonomousAppGenerationEndpoints(app);
    app.MapMcpIntegrationEndpoints();
    app.MapGet("/health/obscura", async (IObscuraHealthService health, CancellationToken ct) =>
    {
        var status = await health.CheckAsync(ct).ConfigureAwait(false);
        return status.GrpcHealthy || status.CdpHealthy
            ? Results.Ok(status)
            : Results.StatusCode(503);
    }).WithTags("Obscura Health");

    app.MapGet("/health/agent-stack", async (IAgentStackHealthService health, CancellationToken ct) =>
    {
        var status = await health.CheckAsync(ct).ConfigureAwait(false);
        return status.AllRequiredHealthy
            ? Results.Ok(new
            {
                healthy = true,
                obscura = status.ObscuraHealthy,
                shadowSync = status.ShadowSyncHealthy,
                sandboxController = status.SandboxControllerHealthy,
                securityScanner = status.SecurityScannerHealthy,
                qdrant = status.QdrantHealthy,
                components = status.Components
            })
            : Results.Json(new
            {
                healthy = false,
                obscura = status.ObscuraHealthy,
                shadowSync = status.ShadowSyncHealthy,
                sandboxController = status.SandboxControllerHealthy,
                securityScanner = status.SecurityScannerHealthy,
                qdrant = status.QdrantHealthy,
                components = status.Components
            }, statusCode: 503);
    }).WithTags("Agent Stack Health");

    if (!app.Environment.IsEnvironment("Testing"))
    {
        var port = Environment.GetEnvironmentVariable("PORT") ?? "5199";
        app.Urls.Add($"http://localhost:{port}");
        Log.Information("Application started successfully on port {Port}", port);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
