using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Rules;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Templates;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.StackStrategy;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime.Docker;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime.Stubs;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Analysis;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Artifacts;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Handoff;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Mcp;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Manager;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.MultiRepo;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.PlanAgent;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Security;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Skills;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Teams;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Unix;
using Libr4.IDE.AutonomousAppGeneration.Agents;
using Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;

/// <summary>
/// Composition root for the AutonomousAppGeneration feature.
///
/// Default wiring:
///   * Docker-backed <see cref="IIsolatedRuntime"/> for real isolation.
///   * <see cref="VmWorkspacePool"/> so multiple workspaces can share one
///     long-living runtime session (classic VM hosting several workspaces).
///   * <see cref="FileSystemWorkspaceSyncService"/> emits change events back
///     to IDE clients whenever the guest modifies files (bidirectional sync
///     via bind-mount).
///
/// Any registration can be replaced by the caller before <c>BuildServiceProvider</c>
/// (e.g. swap Docker for Hyper-V once the VM runtime lands).
/// </summary>
public static class AutonomousAppGenerationDependencyInjection
{
    public static IServiceCollection AddAutonomousAppGeneration(
        this IServiceCollection services,
        string? runtimeProvider = null,
        bool allowFallbackToProcess = true)
    {
        // Orchestrator state + LLM-backed services.
        services.AddSingleton<IAppGenerationRepository, InMemoryAppGenerationRepository>();
        services.AddScoped<IAppPlannerService, LlmAppPlannerService>();
        services.AddScoped<ICodeGenerationService, LlmCodeGenerationService>();
        services.AddScoped<IErrorAnalysisService, LlmErrorAnalysisService>();
        services.AddSingleton<IExecutionManifestBuilder, ExecutionManifestBuilder>();
        services.AddSingleton<IAutonomousRunControlService, AutonomousRunControlService>();
        services.AddSingleton<IAutonomousQualityGateService, AutonomousQualityGateService>();
        services.AddSingleton<IAutonomousCodeConsistencyValidator, AutonomousCodeConsistencyValidator>();
        // P1-2 / P1-10 of audit roadmap: deterministic classifiers / validators.
        services.AddSingleton<IExecutionFailureClassifier, DefaultExecutionFailureClassifier>();
        services.AddSingleton<IPlanCommandValidator, DefaultPlanCommandValidator>();
        // P1-1: Roslyn-based architecture rules.
        services.AddSingleton<IArchitectureCheckRule, AuthImplementationRule_DotNet>();
        // P2-6: Rust sidecar abstraction. Default is no-op until the real sidecar ships.
        services.TryAddSingleton<IRustAnalysisSidecar, NullRustAnalysisSidecar>();
        // P2-7: F# DU rules engine — cross-stack and stack-specific rules via FSharpRulesAdapter.
        services.AddSingleton<IArchitectureCheckRule>(_ => new FSharpRulesAdapter("error_handling"));
        services.AddSingleton<IArchitectureCheckRule>(_ => new FSharpRulesAdapter("observability_baseline"));
        services.AddSingleton<IArchitectureCheckRule>(_ => new FSharpRulesAdapter("semantic_security"));
        // P1-3 of audit roadmap: pipeline-stage scaffolding (strangler-fig). Stages registered here
        // become available for stage-driven orchestration; legacy Handle still owns the loop today.
        // Scoped because some stages depend on scoped services (e.g. IAppPlannerService).
        services.AddScoped<IGenerationStage, IdempotencyCheckStage>();
        services.AddScoped<IGenerationStage, PlanGenerationStage>();
        services.AddScoped<IGenerationStage, PlanCommandValidationStage>();
        services.AddScoped<IGenerationStage, PlanQualityGateStage>();
        services.AddScoped<IGenerationPipelineRunner, DefaultGenerationPipelineRunner>();
        // P1-5 of audit roadmap: per-run LLM budget enforcement.
        services.AddSingleton<IBudgetService>(_ => new InMemoryBudgetService(new BudgetOptions()));
        // P1-8 of audit roadmap: parameterised fallback artefact templates.
        services.AddSingleton<IFallbackArtefactTemplateEngine, ScribanFallbackTemplateEngine>();
        // P1-9 of audit roadmap: stack-strategy registry replacing 5 IsXxxPlan duplicates.
        services.AddSingleton<IStackStrategy, DotNetStackStrategy>();
        services.AddSingleton<IStackStrategy, PythonStackStrategy>();
        services.AddSingleton<IStackStrategy, NodeStackStrategy>();
        services.AddSingleton<IStackStrategy, UnknownStackStrategy>();
        services.AddSingleton<IStackStrategyResolver, StackStrategyResolver>();
        // P1-6 of audit roadmap: bounded queue + BackgroundService for memory consolidation.
        services.AddSingleton<IMemoryConsolidationQueue>(_ =>
            new BoundedMemoryConsolidationQueue(new MemoryConsolidationQueueOptions { Capacity = 64 }));
        services.AddHostedService<MemoryConsolidationBackgroundService>();
        services.AddSingleton<IRunQualityAssessmentService, RunQualityAssessmentService>();
        services.AddSingleton<ICheckpointService, InMemoryCheckpointService>();
        services.AddSingleton<ITriggerAdapter, HttpTriggerAdapter>();
        services.AddSingleton<ITriggerAdapter, SlackTriggerAdapter>();
        services.AddSingleton<ITriggerAdapter, LinearTriggerAdapter>();
        services.AddSingleton<ITriggerAdapter, GitHubTriggerAdapter>();
        services.AddSingleton<ITriggerAdapterRouter, TriggerAdapterRouter>();
        services.AddSingleton<IRunMiddleware, DeterministicRunLoggingMiddleware>();
        services.AddSingleton<IAutonomousFinalizationHook, EnsureTerminalStateFinalizationHook>();
        services.AddSingleton<IAgentEventEmitter, AgentEventEmitter>();

        services.AddSingleton<IMcpToolRegistry, DefaultMcpToolRegistry>();
        services.AddSingleton<IMcpExecutionPolicy, DefaultMcpExecutionPolicy>();
        services.AddSingleton<IMcpSessionRouter, DefaultMcpSessionRouter>();
        services.AddSingleton<IMcpToolInvocationService, McpToolInvocationService>();
        services.AddSingleton<IMcpServerPreflight, DefaultMcpServerPreflight>();
        services.AddSingleton<IMcpLaneWatchdog, DefaultMcpLaneWatchdog>();
        services.AddSingleton<IMemoryStore, InMemoryMemoryStore>();
        services.AddSingleton<IVectorMemoryStore, InProcessVectorMemoryStore>();
        services.AddSingleton<ISkillRegistry, DefaultSkillRegistry>();
        services.AddSingleton<ISkillSelectionStrategy, StageBasedSkillSelectionStrategy>();
        services.AddSingleton<ISkillRunner, SkillRunner>();
        services.AddSingleton<SkillSchemaValidator>();
        services.AddSingleton<IAutonomousCascadePlanner, AutonomousCascadePlanner>();
        services.AddSingleton<IAgentTaskGraphService, AgentTaskGraphService>();
        services.AddSingleton<ISecurityReviewGateService, SecurityReviewGateService>();
        services.AddSingleton<IReviewGate2Service, ReviewGate2Service>();
        services.AddSingleton<IPromptContractService, PromptContractService>();
        services.AddSingleton<IFinalReportService, FinalReportService>();
        services.AddSingleton<IAdaptiveReplannerService, AdaptiveReplannerService>();
        services.AddSingleton<ITaskEvidenceLinkageService, TaskEvidenceLinkageService>();
        services.AddScoped<IFrontendDesignPreplannerService, FrontendDesignPreplannerService>();
        services.AddSingleton<IDesignArtifactService, DesignArtifactService>();
        services.AddSingleton<IDesignArtifactGenerationBindingService, DesignArtifactGenerationBindingService>();
        services.AddSingleton<IContextPackBuilder, ContextPackBuilder>();
        services.AddSingleton<IAgentIntegrationCoordinator, AgentIntegrationCoordinator>();
        services.AddSingleton<IProviderCapabilityMatrix, DefaultProviderCapabilityMatrix>();
        services.AddSingleton<IDiagnosticsBundleService, DiagnosticsBundleService>();
        services.AddSingleton<IArtifactGenerator, ArtifactGenerator>();
        services.AddSingleton<IMcpAdapterRegistry, McpAdapterRegistry>();
        services.AddSingleton<IFlowModeOrchestrator, FlowModeOrchestrator>();
        services.AddSingleton<IMultiRepoWorkspaceRegistry, MultiRepoWorkspaceRegistry>();
        services.AddSingleton<IManagerSurfaceService, ManagerSurfaceService>();
        services.AddSingleton<IUnixComposableTaskRunner, UnixComposableTaskRunner>();
        services.AddScoped<IPlanAgentService, PlanAgentService>();
        services.AddSingleton<ILocalCloudHandoffService, LocalCloudHandoffService>();
        services.AddSingleton<IUserRoleProvider>(_ => new EnvironmentUserRoleProvider(UserRole.ExternalUser));
        services.AddSingleton<ISkillPackRepository, InMemorySkillPackRepository>();
        services.AddSingleton<ISkillPackGovernanceService, SkillPackGovernanceService>();
        services.AddSingleton<ITeamTemplateRepository, InMemoryTeamTemplateRepository>();
        services.AddSingleton<ITeamTemplateResolver, TeamTemplateResolver>();
        services.AddSingleton<ISubagentRoutingService, SubagentRoutingService>();
        services.AddSingleton<ISubagentProfileRepository, InMemorySubagentProfileRepository>();
        services.AddSingleton<ISubagentSelector, SubagentSelector>();

        // Isolation stack with provider selection + fallback.
        services.AddSingleton<IRuntimeDiagnostics, InMemoryRuntimeDiagnostics>();
        services.AddSingleton<IRuntimeCommandPolicy, DefaultRuntimeCommandPolicy>();
        services.AddSingleton<DockerIsolatedRuntime>();
        services.AddSingleton<WslIsolatedRuntime>();
        services.AddSingleton<HyperVIsolatedRuntime>();
        services.AddSingleton<ProcessIsolatedRuntime>();
        services.AddSingleton<IIsolatedRuntime>(sp =>
            new RuntimeProviderRouter(
                preferredProvider: runtimeProvider ?? "docker",
                allowFallbackToProcess: allowFallbackToProcess,
                docker: sp.GetRequiredService<DockerIsolatedRuntime>(),
                wsl: sp.GetRequiredService<WslIsolatedRuntime>(),
                hyperV: sp.GetRequiredService<HyperVIsolatedRuntime>(),
                process: sp.GetRequiredService<ProcessIsolatedRuntime>(),
                diagnostics: sp.GetRequiredService<IRuntimeDiagnostics>(),
                logger: sp.GetRequiredService<ILogger<RuntimeProviderRouter>>()));
        services.AddSingleton<IWorkspacePool, VmWorkspacePool>();
        services.AddSingleton<IWorkspaceSyncService, FileSystemWorkspaceSyncService>();

        // Shadow execution = pool + runtime + sync.
        services.AddSingleton<IShadowExecutionService, IsolatedShadowExecutionService>();

        // New agent infrastructure - register manually when needed with required parameters
        // services.AddSingleton<HierarchicalSkillLoader>();

        // New agents (registered with skill paths)
        services.AddScoped<DatabaseDesignAgent>(sp => new DatabaseDesignAgent(
            Path.Combine(AppContext.BaseDirectory, "Agents", "Skills", "database-designer", "SKILL.md"),
            sp.GetRequiredService<IAIService>(),
            sp.GetRequiredService<ILogger<DatabaseDesignAgent>>()));
        
        services.AddScoped<CICDPipelineAgent>(sp => new CICDPipelineAgent(
            Path.Combine(AppContext.BaseDirectory, "Agents", "Skills", "ci-cd-pipeline-builder", "SKILL.md"),
            sp.GetRequiredService<IAIService>(),
            sp.GetRequiredService<ILogger<CICDPipelineAgent>>()));
        
        services.AddScoped<PerformanceProfilingAgent>(sp => new PerformanceProfilingAgent(
            Path.Combine(AppContext.BaseDirectory, "Agents", "Skills", "performance-profiler", "SKILL.md"),
            sp.GetRequiredService<IAIService>(),
            sp.GetRequiredService<ILogger<PerformanceProfilingAgent>>()));
        
        services.AddScoped<TechDebtTrackingAgent>(sp => new TechDebtTrackingAgent(
            Path.Combine(AppContext.BaseDirectory, "Agents", "Skills", "tech-debt-tracker", "SKILL.md"),
            sp.GetRequiredService<IAIService>(),
            sp.GetRequiredService<ILogger<TechDebtTrackingAgent>>()));
        
        services.AddScoped<ObservabilityAgent>(sp => new ObservabilityAgent(
            Path.Combine(AppContext.BaseDirectory, "Agents", "Skills", "observability-designer", "SKILL.md"),
            sp.GetRequiredService<IAIService>(),
            sp.GetRequiredService<ILogger<ObservabilityAgent>>()));

        return services;
    }
}
