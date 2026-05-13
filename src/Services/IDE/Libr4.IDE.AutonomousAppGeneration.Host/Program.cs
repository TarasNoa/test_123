using Libr4.AI.Application.Abstractions;
using Libr4.AI.Infrastructure.AI;
using Libr4.AI.Infrastructure.AI.Providers;
using Libr4.IDE.Application.AutonomousAppGeneration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Commands;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.AutonomousAppGeneration.Host.Endpoints;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

try
{
    Log.Information("Starting AutonomousAppGeneration Host");
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
    builder.Services.AddHttpClient();
    builder.Services.AddSingleton<OpenRouterProvider>();
    builder.Services.AddSingleton<AlibabaCloudProvider>();
    builder.Services.AddSingleton<DockerModelRunnerProvider>();
    builder.Services.AddSingleton<OllamaProvider>();
    builder.Services.AddSingleton<AIProviderFactory>();
    // P1-5: circuit breaker must be registered before AIService (it's a constructor dependency).
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
    builder.Services.AddAutonomousAppGeneration(runtimeProvider, allowProcessFallback);

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapGet("/", () => Results.Redirect("/swagger"));
    app.MapAutonomousAppGenerationEndpoints();
    app.MapMcpIntegrationEndpoints();

    var port = Environment.GetEnvironmentVariable("PORT") ?? "5200";
    app.Urls.Add($"http://localhost:{port}");

    Log.Information("Application started successfully on port {Port}", port);
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
