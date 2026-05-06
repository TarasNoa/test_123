using Microsoft.AspNetCore.Mvc;
using Libr4.IDE.Application.Commands;
using Libr4.IDE.Application.Queries;
using Libr4.IDE.Application.DTOs;
// using Libr4.IDE.Domain.Algorithms;
using Libr4.AI.Infrastructure.AI;
// using Libr4.IDE.Application.AI.Algorithms;
using Libr4.IDE.Application.Translation;
using Libr4.IDE.Application.Terminal;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Microsoft.FSharp.Control;
using Libr4.IDE.Api;
using Libr4.IDE.Application.Obscura;
// using Libr4.IDE.Infrastructure.FSharpInterop;  // F# interop not yet implemented
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
        policy.WithOrigins("http://localhost:3000") // Frontend port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services
builder.Services.AddSingleton<TerminalWebSocketHandler>();
builder.Services.AddSingleton<AgentEventWebSocketHandler>();
builder.Services.AddScoped<IDockerService, ProcessDockerService>();
builder.Services.AddScoped<ITranslationService, OpenAITranslationService>();
builder.Services.AddScoped<ITerminalService, DockerTerminalService>();
builder.Services.AddScoped<IAgentEventEmitter, AgentEventEmitter>();
builder.Services.AddScoped<IAgentOrchestrationTracker, AgentOrchestrationTracker>();
// Shadow Workspace - Golden Stack Architecture
// Rust (obscura/crates/container-runtime) handles low-level Docker operations
// F# (Libr4.IDE.Domain.FSharp) provides domain logic (state machine, resource allocation)
// C# provides thin orchestration layer
builder.Services.AddSingleton<IContainerManager, ContainerRuntimeGrpcClient>();
builder.Services.AddSingleton<IPreWarmedContainerPool, NullPreWarmedContainerPool>();
builder.Services.AddSingleton<IContainerLifecycleService, ContainerLifecycleBridge>();
builder.Services.AddHostedService<ContainerPoolWarmupService>();

// Obscura Browser Automation - Golden Stack: Rust chromiumoxide via gRPC
builder.Services.AddSingleton<IBrowserAutomationService, BrowserAutomationGrpcClient>();
builder.Services.AddSingleton<IAgentObscuraTool, AgentObscuraTool>(); // Will use Rust service internally
builder.Services.AddSingleton<IDomToMarkdownConverter, DomToMarkdownConverter>();
builder.Services.AddSingleton<ISubagentObscuraIntegration, SubagentObscuraIntegration>();

// Semantic Code Index (SocratiCode analog) - Ollama embeddings + Qdrant vector store + BM25 RRF
builder.Services.AddSemanticCodeIndex(builder.Configuration);

// Git Automation - LibGit2Sharp
builder.Services.AddScoped<IGitAutomationService, LibGit2SharpService>();
builder.Services.AddScoped<ICodeSearchService, CodeSearchService>();
builder.Services.AddScoped<IPromptOptimizationService, PromptOptimizationService>();
// Design context service (awesome-design-md style internal implementation)
builder.Services.AddScoped<IDesignContextService, DesignContextService>();
// Design skills service (TypeUI Design Skills style internal implementation)
builder.Services.AddScoped<IDesignSkillsService, DesignSkillsService>();

// Multi-Agent Debate & Context Compression
builder.Services.AddSingleton<IContextCompressionService, ContextCompressionService>();
builder.Services.AddSingleton<IAgentDebateService, AgentDebateService>();

// F# Interop services - commented out due to missing AddFSharpInterop
// builder.Services.AddFSharpInterop();  // Agent State Machine, Consensus, AST Transforms

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {});
builder.Services.AddSignalR();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateCodeSessionCommand).Assembly));
// AI command handlers - commented out due to missing ChatCommand
// builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.AI.Commands.ChatCommand).Assembly));
// Obscura browser MediatR handlers
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.Obscura.Commands.LaunchBrowserCommand).Assembly));
// TaskDecomposition - commented out due to missing namespace
// builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.TaskDecomposition.Commands.DecomposeTaskCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.SeniorRolePrompts.Commands.GenerateRolePromptCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.IntelligenceRouter.Commands.BuildRoutingPlanCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.Cascade.Commands.RunCascadePlanningCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.OrchestrationRun.Commands.StartOrchestrationRunCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Libr4.IDE.Application.MultiAgentOrchestration.Commands.StartAgentOrchestrationCommand).Assembly));
builder.Services.AddSingleton<IContextCompressionService, ContextCompressionService>();  // HiveMind context compression
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
builder.Services.AddScoped<IAIService, Libr4.AI.Infrastructure.AI.AIService>();
builder.Services.AddHttpClient();

// 3. PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Sandbox Orchestrator for production-ready task execution
builder.Services.AddScoped<Libr4.IDE.Application.Orchestration.SandboxOrchestrator>();
builder.Services.AddScoped<Libr4.IDE.Infrastructure.Clients.ISandboxClient, Libr4.IDE.Infrastructure.Clients.GrpcSandboxClient>();

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

// CORS already configured above with specific origin

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(options => {});
app.MapControllers();

// Obscura browser endpoints (external integration)
app.MapObscuraEndpoints();

// AI Assistant endpoints - commented out due to missing endpoint file
// app.MapAIEndpoints();

// Task Decomposition endpoints - commented out due to missing namespace
// app.MapTaskDecompositionEndpoints();

// Senior Role Prompts
app.MapSeniorRolePromptsEndpoints();

// Intelligence Router
app.MapIntelligenceRouterEndpoints();

// Cascade planning endpoint
app.MapCascadeEndpoints();

// Orchestration Run
app.MapOrchestrationRunEndpoints();

// Multi-Agent Orchestration endpoints - commented out due to missing endpoint file
// app.MapMultiAgentOrchestrationEndpoints();

// Autonomous Runtime Policy
app.MapAutonomousRuntimePolicyEndpoints();

// Shadow Workspace
app.MapShadowWorkspaceEndpoints();

// Code Review endpoints - commented out due to missing endpoint file
// app.MapCodeReviewEndpoints();

// Semantic Code Graph
app.MapSemanticCodeGraphEndpoints();

// Agent Memory System endpoints - commented out due to missing endpoint file
// app.MapAgentMemorySystemEndpoints();

// Agent Instance and Specialization endpoints - commented out due to missing endpoint file
// app.MapAgentEndpoints();

// LLM Router - commented out due to missing endpoint file
// app.MapLLMRouterEndpoints();

// AI Workflow Automation
app.MapAIWorkflowAutomationEndpoints();

// AI Workflow endpoints - commented out due to missing endpoint file
// app.MapAIWorkflowEndpoints();

// WebSearch endpoints - commented out due to missing endpoint file
// app.MapWebSearchEndpoints();

// Task Record - commented out due to missing endpoint file
// app.MapTaskRecordEndpoints();

// GitHubBootstrap endpoints - commented out due to missing endpoint file
// app.MapGitHubBootstrapEndpoints();

// Architectural Guardrails
app.MapArchitecturalGuardrailsEndpoints();

// Semantic Blame
app.MapSemanticBlameEndpoints();

// CodeIntelligence endpoints - commented out due to missing endpoint file
// app.MapCodeIntelligenceEndpoints();

// Security Testing endpoints
app.MapSecurityTestingEndpoints();

// HackerAgent endpoints - commented out due to missing endpoint file
// app.MapHackerAgentEndpoints();

// Golden Stack: Agent State endpoints for Frontend synchronization
app.MapAgentStateEndpoints();

// F# Interop demo endpoint - Agent State Machine - commented out due to missing IAgentStateMachineBridge
/*
app.MapGet("/api/fsharp/agent-demo", (IAgentStateMachineBridge bridge) =>
{
    // Create agent
    var agent = bridge.CreateIdleState("demo-agent-123", new[] { "code", "test", "review" });
    
    // Initialize
    var initialized = bridge.Initialize(agent, new Dictionary<string, object> 
    { 
        ["workspace"] = "ws-456",
        ["context"] = "demo"
    });
    
    // Mark ready
    var ready = bridge.MarkReady(initialized, new[] { "git", "dotnet", "docker" });
    
    return Results.Ok(new 
    { 
        AgentId = bridge.GetAgentId(ready),
        State = bridge.GetStateName(ready),
        CanAcceptTask = bridge.CanAcceptTask(ready),
        IsActive = bridge.IsActive(ready),
        Progress = bridge.GetProgress(ready),
        Message = "F# Agent State Machine via C# Bridge - Golden Stack!"
    });
});
*/

// Terminal - не реализован
// app.MapTerminalEndpoints();

// Translation endpoints - commented out due to missing endpoint file
// TODO: Fix endpoint compilation errors
// app.MapTranslationEndpoints();

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

app.MapPost("/api/ide/sessions/{sessionId}/participants", async (Guid sessionId, AddParticipantCommand command, IMediator mediator) =>
{
    command = command with { SessionId = sessionId };
    await mediator.Send(command);
    return Results.Ok();
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

// SignalR Hub for Shadow Workspace real-time collaboration - commented out due to missing ShadowWorkspaceHub
// app.MapHub<ShadowWorkspaceHub>("/hubs/shadow-workspace");

app.Run();
