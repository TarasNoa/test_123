using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Libr4.IDE.Infrastructure.Persistence;
using Libr4.IDE.Infrastructure.Orchestration;
using Libr4.IDE.Application.Commands;
using Libr4.IDE.Application.Queries;
using Libr4.IDE.Application.DTOs;
// using Libr4.IDE.Domain.Algorithms;
using Libr4.AI.Infrastructure.AI;
using Libr4.AI.Infrastructure;
// using Libr4.IDE.Application.AI.Algorithms;
using Libr4.IDE.Application.Translation;
using Libr4.IDE.Application.Terminal;
using Libr4.IDE.Application.AgentEvents;
using Libr4.IDE.Application.AutonomousAppGeneration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;
using Libr4.IDE.Application.AutonomousAppGeneration.Api;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Microsoft.FSharp.Control;
using Libr4.IDE.Api;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Application.GitAutomation;
using Libr4.IDE.Application.CodeSearch;
using Libr4.IDE.Application.PromptOptimization;
using Libr4.IDE.Infrastructure.SemanticIndex;
using Libr4.IDE.Infrastructure.Persistence;
using Libr4.IDE.Application.SecurityTesting;
using Libr4.IDE.Application.CodeReview;
using Libr4.IDE.Application.Escrow;
using Libr4.IDE.Application.Gateway;
using Libr4.IDE.Application.AI;
using Libr4.IDE.Application.ShadowWorkspace;
using Libr4.IDE.Application.MultiAgentOrchestration;
using Libr4.IDE.Application.DesignContext;
using Libr4.IDE.Application.DesignSkills;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Domain;
using Libr4.IDE.Domain.AI;
using Libr4.IDE.Infrastructure.Sandbox;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure JSON for F# support and pretty output
builder.Services.ConfigureHttpJsonOptions(options => {
    // Add support for enum-like structures (important for F#)
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// 2. Configure CORS for Frontend (SolidJS/Next.js)
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? new[] { "http://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // нужно для SignalR
    });
});

// Add services
builder.Services.AddScoped<TerminalWebSocketHandler>();
builder.Services.AddSingleton<AgentEventWebSocketHandler>();
builder.Services.AddSingleton<RunSyncWebSocketHandler>();
builder.Services.AddHostedService<AgentRuntimeWebSocketBridge>();
builder.Services.AddScoped<IDockerService, ProcessDockerService>();
builder.Services.AddScoped<ITranslationService, OpenAITranslationService>();
builder.Services.AddScoped<ITerminalService, DockerTerminalService>();
builder.Services.AddScoped<IAgentEventEmitter, AgentEventEmitter>();
builder.Services.AddSingleton<IAgentOrchestrationBroadcaster, AgentOrchestrationWebSocketBroadcaster>();
builder.Services.AddScoped<IAgentOrchestrationTracker, AgentOrchestrationTracker>();
builder.Services.AddScoped<ICodeSessionRepository, InMemoryCodeSessionRepository>();
builder.Services.AddSingleton<IAgentStreamEmitter, AgentStreamEmitter>();
builder.Services.AddScoped<IAgentSpawnerService, AgentSpawnerService>();
builder.Services.AddHealthChecks();

// Shadow Workspace - Golden Stack Architecture
builder.Services.AddSingleton<Libr4.IDE.Application.ShadowWorkspace.ICrdtDocumentService, Libr4.IDE.Application.ShadowWorkspace.CrdtDocumentService>();
builder.Services.AddSingleton<Libr4.IDE.Infrastructure.Containers.IContainerManager, Libr4.IDE.Infrastructure.Containers.ContainerManager>();
builder.Services.AddSingleton<Libr4.IDE.Application.ShadowWorkspace.IContainerManager, Libr4.IDE.Infrastructure.ShadowWorkspace.ContainerManagerAdapter>();
builder.Services.AddSingleton<Libr4.IDE.Infrastructure.Containers.IPreWarmedContainerPool, Libr4.IDE.Infrastructure.Containers.PreWarmedContainerPool>();
builder.Services.AddSingleton<Libr4.IDE.Application.ShadowWorkspace.IPreWarmedContainerPool, Libr4.IDE.Infrastructure.ShadowWorkspace.PreWarmedContainerPoolAdapter>();
builder.Services.AddSingleton<Libr4.IDE.Application.ShadowWorkspace.IContainerLifecycleService, Libr4.IDE.Application.ShadowWorkspace.ContainerLifecycleBridge>();
builder.Services.AddHostedService<Libr4.IDE.Infrastructure.Containers.ContainerPoolWarmupService>();

// Obscura Browser Automation - Golden Stack: Rust chromiumoxide via gRPC
builder.Services.AddObscuraBrowserPlane(builder.Configuration);
builder.Services.AddObscuraSessionHostedServices();
builder.Services.AddSingleton<IAgentOrchestrator, Libr4.IDE.Application.MultiAgentOrchestration.MultiAgentOrchestrator>();

// AI Infrastructure (required by UnifiedChatEndpoints)
builder.Services.AddAIInfrastructure(builder.Configuration);

// Autonomous app generation (IDE chat → StartAppGeneration, gateway /api/v1/ide/app-generation/*)
builder.Services.AddAutonomousAppGeneration(
    builder.Configuration["AutonomousAppGeneration:RuntimeProvider"],
    builder.Configuration.GetValue("AutonomousAppGeneration:AllowProcessFallback", true),
    builder.Configuration);
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.AutonomousAppGeneration.Commands.StartAppGenerationCommand).Assembly));
builder.Services.AddSingleton<IAppGenerationChatBridge, AppGenerationChatBridge>();

// Semantic Code Index (SocratiCode analog) - Ollama embeddings + Qdrant vector store + BM25 RRF
builder.Services.AddSemanticCodeIndex(builder.Configuration);

// Git Automation - LibGit2Sharp
builder.Services.AddScoped<IGitAutomationService, LibGit2SharpService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<ICodeSearchService, CodeSearchService>();
builder.Services.AddScoped<IPromptOptimizationService, PromptOptimizationService>();
// Design context service (awesome-design-md style internal implementation)
builder.Services.AddScoped<IDesignContextService, DesignContextService>();
// Design skills service (TypeUI Design Skills style internal implementation)
builder.Services.AddScoped<IDesignSkillsService, DesignSkillsService>();

// Multi-Agent Debate & Context Compression
builder.Services.AddSingleton<IContextCompressionService, ContextCompressionService>();
builder.Services.AddSingleton<IAgentDebateService, AgentDebateService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {});

// SignalR with JWT authentication and F# serialization
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
}).AddJsonProtocol(options =>
{
    // Configure JSON serialization for F# Discriminated Unions
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:SigningKey"]
                ?? builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT signing key not configured. Set Jwt__SigningKey environment variable.")
            ))
        };

        // Allow JWT token in SignalR QueryString (WebSocket doesn't support headers)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateCodeSessionCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.AI.Commands.ChatCommand).Assembly));
// Obscura browser MediatR handlers
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.Obscura.Commands.LaunchBrowserCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.SeniorRolePrompts.Commands.GenerateRolePromptCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.IntelligenceRouter.Commands.BuildRoutingPlanCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.Cascade.Commands.RunCascadePlanningCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.OrchestrationRun.Commands.StartOrchestrationRunCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.MultiAgentOrchestration.Commands.StartAgentOrchestrationCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.AutonomousRuntimePolicy.Commands.GeneratePolicyCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.ShadowWorkspace.Commands.CreateShadowWorkspaceCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.CodeReview.Commands.RunCodeReviewCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.SemanticCodeGraph.Commands.BuildGraphCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.GitHubBootstrap.Commands.BootstrapProjectCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.CodeIntelligence.Commands.GetCompletionsCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.AgentMemorySystem.Commands.CreateMemoryCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.LLMRouter.Commands.RouteLLMCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.ArchitecturalGuardrails.Commands.RunValidationCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.WebSearch.Commands.ExecuteSearchCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.TaskRecord.Commands.CreateTaskRecordCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.SemanticBlame.Commands.RunBlameCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.SecurityTesting.Commands.RunSecurityTestCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.HackerAgent.Commands.RunHackerAgentCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.AIWorkflowAutomation.Commands.DistillWorkflowCommand).Assembly));
builder.Services.AddSingleton<IQualityGateService, QualityGateService>();
builder.Services.AddScoped<IAIConversationRepository, Libr4.IDE.Infrastructure.Persistence.EfAIConversationRepository>();
builder.Services.AddScoped<ICodeSessionRepository, InMemoryCodeSessionRepository>();
builder.Services.AddSingleton<Libr4.IDE.Infrastructure.Sandbox.ISandboxClient, Libr4.IDE.Infrastructure.Sandbox.RustSandboxExecutor>();
builder.Services.AddHttpClient();

// 3. PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Регистрируем IdeDbContext для репозиториев (используют IDbContextFactory)
builder.Services.AddDbContextFactory<IdeDbContext>(options =>
    options.UseNpgsql(connectionString));

// Регистрируем репозитории
builder.Services.AddScoped<IAgentEventRepository, EfAgentEventRepository>();
builder.Services.AddScoped<IAgentOrchestrationRepository, EfAgentOrchestrationRepository>();
builder.Services.AddScoped<IAppGenerationEntityRepository, AppGenerationRepository>();

// Sandbox Orchestrator for production-ready task execution - moved to Infrastructure
builder.Services.AddScoped<AgentOrchestrator>();
builder.Services.AddScoped<Libr4.IDE.Infrastructure.Clients.ISandboxClient, Libr4.IDE.Infrastructure.Clients.GrpcSandboxClient>();

// Code Guardian for validation before Rust execution
builder.Services.AddScoped<Libr4.IDE.Application.Security.ICodeValidator, Libr4.IDE.Application.Security.CodeGuardian>();

// Execution Cache for memoization (SHA-256 based)
builder.Services.AddSingleton<Libr4.IDE.Application.Caching.IExecutionCache, Libr4.IDE.Application.Caching.ExecutionCache>();

// Shadow workspace services already registered above
builder.Services.AddScoped<ISelfHealingBuildPipeline, SelfHealingBuildPipeline>();
builder.Services.AddScoped<ISecurityTestingService, SecurityTestingService>();
builder.Services.AddScoped<ICodeReviewService, CodeReviewService>();
builder.Services.AddScoped<IEscrowCodeService, EscrowCodeService>();
builder.Services.AddScoped<IGatewayPreviewIntegration, GatewayPreviewIntegration>();

// AutonomousAppGeneration orchestrator lives in its own project
// (Libr4.IDE.AutonomousAppGeneration) and is exposed via Libr4.IDE.AutonomousAppGeneration.Host,
// decoupled from the legacy IDE.Application compile errors.

builder.Services.AddScoped<IAIAlgorithmService, AIAlgorithmServiceWrapper>();
builder.Services.AddSingleton<Libr4.AI.Infrastructure.AI.AIProviderFactory>();
builder.Services.AddSingleton<Libr4.AI.Infrastructure.AI.LlmCircuitBreaker>();
builder.Services.AddSingleton<Libr4.AI.Infrastructure.Hooks.HookManager>();
builder.Services.AddSingleton<Libr4.AI.Infrastructure.LLMRouter>();

// Validators used in minimal API endpoints
builder.Services.AddScoped<Libr4.IDE.Application.CodeReview.Validators.RunCodeReviewCommandValidator>();
builder.Services.AddScoped<Libr4.IDE.Application.MultiAgentOrchestration.Validators.StartAgentOrchestrationCommandValidator>();

// CORS already configured above with specific origin

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2),
});
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(options => {});
app.MapHealthChecks("/health");
app.MapControllers();

app.MapObscuraEndpoints();

app.MapAIEndpoints();

app.MapTaskDecompositionEndpoints();

app.MapSeniorRolePromptsEndpoints();

app.MapIntelligenceRouterEndpoints();

app.MapCascadeEndpoints();

app.MapOrchestrationRunEndpoints();

app.MapMultiAgentOrchestrationEndpoints();

app.MapAutonomousRuntimePolicyEndpoints();

// Shadow Workspace
app.MapShadowWorkspaceEndpoints();

app.MapCodeReviewEndpoints();

app.MapSemanticCodeGraphEndpoints();

app.MapAgentMemorySystemEndpoints();

app.MapAgentEndpoints();

app.MapLLMRouterEndpoints();

app.MapAIWorkflowAutomationEndpoints();

app.MapAIWorkflowEndpoints();

app.MapWebSearchEndpoints();

app.MapTaskRecordEndpoints();

app.MapGitHubBootstrapEndpoints();

app.MapArchitecturalGuardrailsEndpoints();

app.MapSemanticBlameEndpoints();

app.MapCodeIntelligenceEndpoints();

app.MapSecurityTestingEndpoints();

app.MapHackerAgentEndpoints();

// Golden Stack: Agent State endpoints for Frontend synchronization
app.MapAgentStateEndpoints();

// Unified Chat — Q&A or autonomous app generation (same path users type in IDE)
app.MapUnifiedChatEndpoints();
app.MapAutonomousAppGenerationEndpoints("/api/v1/ide/app-generation");
app.MapAgentFleetEndpoints("/api/v1/ide/agent-fleet");
app.MapGitHubCiWebhookEndpoints("/api/v1/ide/webhooks/github");
app.MapAgentSpaceEndpoints("/api/v1/ide/spaces");
Libr4.IDE.Application.AutonomousAppGeneration.Api.SessionSearchEndpoints.MapSessionSearchEndpoints(app, "/api/v1/ide/memory");

// SignalR Hub for real-time agent updates
app.MapHub<Libr4.IDE.Api.Hubs.AgentHub>("/hubs/agents");

app.MapTerminalEndpoints();

app.MapTranslationEndpoints();

// Code Sessions endpoints
app.MapPost("/api/ide/sessions", async (CreateCodeSessionCommand command, IMediator mediator) =>
{
    var sessionId = await mediator.Send<Guid>(command);
    return Results.Ok(new { sessionId });
});

app.MapGet("/api/ide/sessions/{id}", async (Guid id, IMediator mediator) =>
{
    var session = await mediator.Send<CodeSessionDto?>(new GetCodeSessionQuery(id));
    return session is not null ? Results.Ok(session) : Results.NotFound();
});

app.MapPost("/api/ide/sessions/{sessionId}/files", async (Guid sessionId, AddFileToSessionCommand command, IMediator mediator) =>
{
    command = command with { SessionId = sessionId };
    await mediator.Send(command);
    return Results.Ok();
});

app.MapPut("/api/ide/sessions/{sessionId}/files/{fileId}", async (Guid sessionId, Guid fileId, [FromBody] UpdateFileCommand command, IMediator mediator) =>
{
    command = command with { SessionId = sessionId, FileId = fileId };
    await mediator.Send(command);
    return Results.Ok();
});

app.MapGet("/api/ide/sessions/{sessionId}/files/{fileId}/content", async (
    Guid sessionId,
    Guid fileId,
    ICodeSessionRepository repo,
    CancellationToken ct) =>
{
    var session = await repo.GetByIdAsync(sessionId, ct);
    if (session is null) return Results.NotFound();
    var file = session.Files.FirstOrDefault(f => f.Id == fileId);
    if (file is null) return Results.NotFound();
    return Results.Ok(new { file.Id, file.FileName, file.Content, file.Language });
});

app.MapDelete("/api/ide/sessions/{sessionId}/files/{fileId}", async (
    Guid sessionId,
    Guid fileId,
    ICodeSessionRepository repo,
    CancellationToken ct) =>
{
    var session = await repo.GetByIdAsync(sessionId, ct);
    if (session is null) return Results.NotFound();
    session.Files.RemoveAll(f => f.Id == fileId);
    await repo.UpdateAsync(session, ct);
    return Results.NoContent();
});

app.MapPost("/api/ide/sessions/{sessionId}/participants", async (Guid sessionId, AddParticipantCommand command, IMediator mediator) =>
{
    command = command with { SessionId = sessionId };
    await mediator.Send(command);
    return Results.Ok();
});

// ─── File operations (v1) — used by Web IDE frontend ───
app.MapGet("/api/v1/ide/files", async ([FromQuery] Guid sessionId, ICodeSessionRepository repo, CancellationToken ct) =>
{
    var session = await repo.GetByIdAsync(sessionId, ct);
    if (session is null) return Results.NotFound();
    var tree = session.Files.Select(f => new
    {
        id = f.FileName,
        path = f.FileName,
        name = Path.GetFileName(f.FileName),
        type = "file",
        language = f.Language ?? "text",
    }).ToList();
    return Results.Ok(tree);
});

app.MapGet("/api/v1/ide/files/content", async ([FromQuery] Guid sessionId, [FromQuery] string path, ICodeSessionRepository repo, CancellationToken ct) =>
{
    var session = await repo.GetByIdAsync(sessionId, ct);
    if (session is null) return Results.NotFound();
    var file = session.Files.FirstOrDefault(f => f.FileName == path);
    if (file is null) return Results.NotFound();
    return Results.Ok(new { content = file.Content });
});

app.MapPost("/api/v1/ide/files/save", async ([FromBody] SaveFileRequest req, ICodeSessionRepository repo, CancellationToken ct) =>
{
    var session = await repo.GetByIdAsync(req.SessionId, ct);
    if (session is null) return Results.NotFound();
    var existing = session.Files.FirstOrDefault(f => f.FileName == req.Path);
    if (existing is null)
    {
        var ext = Path.GetExtension(req.Path).TrimStart('.');
        session.AddFile(req.Path, req.Content, ext);
    }
    else
    {
        existing.UpdateContent(req.Content);
    }
    await repo.UpdateAsync(session, ct);
    return Results.Ok(new { saved = true });
});

// AI Code Assistance endpoints - commented out due to missing Algorithms namespace
/*
app.MapPost("/api/ide/ai/generate", async (GenerateCodeRequest request, IAIService aiService) =>
{
    var result = Microsoft.FSharp.Control.FSharpAsync.RunSynchronously<Libr4.IDE.Domain.Algorithms.CodeAssistant.CodeGenerationResult>(
        Libr4.IDE.Domain.Algorithms.CodeAssistant.generateCodeWithAI(aiService, request.Prompt, request.Language),
        Microsoft.FSharp.Core.FSharpOption<int>.None,
        System.Threading.CancellationToken.None
    );
    return Results.Ok(new GenerateCodeResponse(result.GeneratedCode, result.Explanation, result.Language, result.Confidence));
});

app.MapPost("/api/ide/ai/debug", async (DebugCodeRequest request, IAIService aiService) =>
{
    var issues = Microsoft.FSharp.Control.FSharpAsync.RunSynchronously<Microsoft.FSharp.Collections.FSharpList<Libr4.IDE.Domain.Algorithms.CodeAssistant.DebuggingSuggestion>>(
        Libr4.IDE.Domain.Algorithms.CodeAssistant.debugCodeWithAI(aiService, request.Code, request.Language, request.ErrorMessage),
        Microsoft.FSharp.Core.FSharpOption<int>.None,
        System.Threading.CancellationToken.None
    );
    var dto = issues.Select(i => new DebuggingSuggestionDto(i.Issue, i.SuggestedFix, Microsoft.FSharp.Core.FSharpOption<int>.get_IsSome(i.LineNumber) ? i.LineNumber.Value : (int?)null, i.Severity)).ToList();
    return Results.Ok(new DebugCodeResponse(dto));
});

app.MapPost("/api/ide/ai/complete", async (CompleteCodeRequest request, IAIService aiService) =>
{
    var result = Microsoft.FSharp.Control.FSharpAsync.RunSynchronously<Libr4.IDE.Domain.Algorithms.CodeAssistant.CodeCompletion>(
        Libr4.IDE.Domain.Algorithms.CodeAssistant.completeCodeWithAI(aiService, request.Code, request.Language, request.CursorPosition),
        Microsoft.FSharp.Core.FSharpOption<int>.None,
        System.Threading.CancellationToken.None
    );
    return Results.Ok(new CompleteCodeResponse(result.Completions.ToList(), result.Context, result.Confidence));
});

app.MapPost("/api/ide/ai/optimize", async (OptimizeCodeRequest request, IAIService aiService) =>
{
    var result = Microsoft.FSharp.Control.FSharpAsync.RunSynchronously<Libr4.IDE.Domain.Algorithms.CodeAssistant.CodeOptimization>(
        Libr4.IDE.Domain.Algorithms.CodeAssistant.optimizeCodeWithAI(aiService, request.Code, request.Language),
        Microsoft.FSharp.Core.FSharpOption<int>.None,
        System.Threading.CancellationToken.None
    );
    return Results.Ok(new OptimizeCodeResponse(result.OptimizedCode, result.Improvements.ToList(), result.PerformanceGain));
});

app.MapPost("/api/ide/ai/explain", async (ExplainCodeRequest request, IAIService aiService) =>
{
    var result = Microsoft.FSharp.Control.FSharpAsync.RunSynchronously<Libr4.IDE.Domain.Algorithms.CodeAssistant.CodeExplanation>(
        Libr4.IDE.Domain.Algorithms.CodeAssistant.explainCodeWithAI(aiService, request.Code, request.Language),
        Microsoft.FSharp.Core.FSharpOption<int>.None,
        System.Threading.CancellationToken.None
    );
    return Results.Ok(new ExplainCodeResponse(result.Explanation, result.KeyPoints.ToList(), result.Complexity));
});
*/

// WebSocket for terminal real-time output
app.MapGet("/ws/terminal/{sessionId}", async (string sessionId, TerminalWebSocketHandler handler, HttpContext context) =>
{
    await handler.HandleWebSocketAsync(context, sessionId);
});

// WebSocket for agent events real-time delivery
app.MapGet("/ws/events/{runId}", async (string runId, AgentEventWebSocketHandler handler, HttpContext context) =>
{
    await handler.HandleWebSocketAsync(context, runId);
});

// WebSocket for run handoff live workspace sync (Phase 7.5.3)
app.MapGet("/ws/run-sync/{runId:guid}", async (
    Guid runId,
    RunSyncWebSocketHandler handler,
    HttpContext context,
    string role,
    string workspaceRoot) =>
{
    await handler.HandleWebSocketAsync(context, runId, role, workspaceRoot);
});

app.MapHub<ShadowWorkspaceHub>("/hubs/shadow-workspace");

using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (app.Environment.IsDevelopment())
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();
    }

app.Run();
