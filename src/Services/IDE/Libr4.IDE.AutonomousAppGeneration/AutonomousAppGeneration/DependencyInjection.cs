using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.LspBridge;
using Libr4.IDE.Application.GitAutomation;
using Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;
using Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;
using Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;
using Libr4.IDE.Application.AutonomousAppGeneration.Extensions;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;
using Libr4.IDE.Application.AutonomousAppGeneration.MetaAgent;
using Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;
using Libr4.IDE.Application.AutonomousAppGeneration.Evaluation;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;
using Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Libr4.IDE.Application.AutonomousAppGeneration.HostProfiles;
using Libr4.IDE.Application.AutonomousAppGeneration.InlineCompletion;
using Libr4.IDE.Application.AutonomousAppGeneration.LiveSearch;
using Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Rules;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Templates;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.StackStrategy;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime.Docker;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Analysis;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;
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
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Crystallization;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Consolidation;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Cognitive;
using Libr4.IDE.Application.AutonomousAppGeneration.Computer;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.Spaces;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Search;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Fim;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Unix;
using Libr4.IDE.AutonomousAppGeneration.Agents;
using Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Libr4.AI.Application.Abstractions;
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
        bool allowFallbackToProcess = true,
        IConfiguration? configuration = null,
        bool registerObscuraMediatRHandlers = true)
    {
        // Orchestrator state + LLM-backed services.
        services.AddSingleton<IAppGenerationRepository, InMemoryAppGenerationRepository>();
        services.AddSingleton<IAppGenerationRunStarter, AppGenerationRunStarter>();
        services.AddScoped<IAppPlannerService, LlmAppPlannerService>();
        services.AddScoped<ICodeGenerationService, LlmCodeGenerationService>();
        services.AddSingleton<IFimPromptBuilder, FimPromptBuilder>();
        services.AddScoped<IClaudeCodeStyleRepairService, ClaudeCodeStyleRepairService>();
        services.AddScoped<IErrorAnalysisService, LlmErrorAnalysisService>();
        services.AddSingleton<ITaskGraphHydrationService, TaskGraphHydrationService>();
        services.AddSingleton<IExecutionManifestBuilder, ExecutionManifestBuilder>();
        services.AddSingleton<IAutonomousRunControlService, AutonomousRunControlService>();
        services.AddSingleton<IAutonomousQualityGateService, AutonomousQualityGateService>();
        services.AddSingleton<IAutonomousCodeConsistencyValidator, AutonomousCodeConsistencyValidator>();
        // P1-2 / P1-10 of audit roadmap: deterministic classifiers / validators.
        services.AddSingleton<IExecutionFailureClassifier, DefaultExecutionFailureClassifier>();
        services.AddSingleton<IPlanCommandValidator, DefaultPlanCommandValidator>();
        // P1-1: Roslyn-based architecture rules.
        services.AddSingleton<IArchitectureCheckRule, AuthImplementationRule_DotNet>();
        // P2-6 / Wave 6.1: analysis sidecar — C++ tree-sitter in-process when native lib present, else no-op.
        services.TryAddSingleton<NullRustAnalysisSidecar>();
        services.TryAddSingleton<CppTreeSitterAnalysisSidecar>();
        services.TryAddSingleton<IRustAnalysisSidecar>(sp =>
            CppTreeSitterBridge.IsAvailable
                ? sp.GetRequiredService<CppTreeSitterAnalysisSidecar>()
                : sp.GetRequiredService<NullRustAnalysisSidecar>());
        // P2-7: F# DU rules engine вЂ” cross-stack and stack-specific rules via FSharpRulesAdapter.
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
        services.AddScoped<IGenerationStage, GenerationStage>();
        services.AddScoped<IGenerationStage, SecurityReviewStage>();
        services.AddScoped<IGenerationStage, ReviewGate2Stage>();
        services.AddScoped<IGenerationStage, ConsistencyCheckStage>();
        services.AddScoped<IGenerationStage, StartupBuildStage>();
        services.AddScoped<IGenerationStage, RepairLoopStage>();
        services.AddScoped<IGenerationStage, VerifyStage>();
        services.AddScoped<IGenerationStage, ShipStage>();
        services.AddScoped<IGenerationPipelineRunner, DefaultGenerationPipelineRunner>();
        services.AddScoped<IFullGenerationPipelineRunner, FullGenerationPipelineRunner>();
        // P1-5 of audit roadmap: per-run LLM budget enforcement + provider cost tracking (Phase 6.2).
        services.AddProviderCapabilityMatrix(configuration);
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
        services.AddSingleton<IBuildDiagnosticsDashboardService, BuildDiagnosticsDashboardService>();
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
        services.AddSingleton<IObscuraMcpBridge, ObscuraMcpBridge>();
        services.AddSingleton<IMcpToolInvocationService, McpToolInvocationService>();
        services.AddSingleton<IMcpServerPreflight, DefaultMcpServerPreflight>();
        services.AddSingleton<IMcpLaneWatchdog, DefaultMcpLaneWatchdog>();
        services.AddMcpHost(configuration);
        services.AddLspBridge(configuration);
        services.AddShadowGitCheckpoint(configuration);
        services.AddWorkspaceTrust(configuration);
        services.AddBatchCi(configuration);
        services.AddAgentScheduling(configuration);
        services.AddHermesMemory(configuration);
        services.AddPostRunExtraction(configuration);
        services.AddSkillCrystallization(configuration);
        services.AddQdrantSync(configuration);
        services.AddSessionSearch(configuration);
        services.AddUserProfiles(configuration);
        services.AddHonchoMemory(configuration);
        services.AddAgentSpecEvolution(configuration);
        services.AddFineTuningDataPipeline(configuration);
        services.AddInternalEvalHarness(configuration);
        services.AddLiveSearch(configuration);
        services.AddInlineCompletion(configuration);
        services.AddAgentModelRouting(configuration);
        services.AddAgentStack(configuration);
        services.AddAutonomousHostProfiles(configuration);
        services.AddDreamConsolidation(configuration);
        services.AddCognitiveMemoryBridge();
        services.AddVerifySubagent(configuration);
        services.AddComputerSubagent(configuration);
        services.AddSingleton<ISkillRegistry, DefaultSkillRegistry>();
        services.AddSingleton<ISkillSelectionStrategy, StageBasedSkillSelectionStrategy>();
        services.AddSingleton<ISkillRunner, SkillRunner>();
        services.AddSingleton<SkillSchemaValidator>();
        services.AddSingleton<ICascadeWebPrefetchService, CascadeWebPrefetchService>();
        services.AddSingleton<ICascadeCodebasePrefetchService, CascadeCodebasePrefetchService>();
        services.AddSingleton<IUpstreamCloneProvider, GitUpstreamCloneProvider>();
        services.AddSingleton<IAutonomousCascadePlanner, AutonomousCascadePlanner>();
        services.AddSingleton<IAgentTaskGraphService, AgentTaskGraphService>();
        services.AddScoped<ISecurityReviewGateService, LlmSecurityReviewGateService>();
        services.AddSingleton<IReviewGate2Service, ReviewGate2Service>();
        services.AddSingleton<IPromptContractService, PromptContractService>();
        services.AddSingleton<IFinalReportService, FinalReportService>();
        services.AddSingleton<IAdaptiveReplannerService, AdaptiveReplannerService>();
        services.AddSingleton<ITaskEvidenceLinkageService, TaskEvidenceLinkageService>();
        services.AddScoped<IFrontendDesignPreplannerService, FrontendDesignPreplannerService>();
        services.AddSingleton<IDesignArtifactService, DesignArtifactService>();
        services.AddSingleton<IDesignArtifactGenerationBindingService, DesignArtifactGenerationBindingService>();
        services.AddSingleton<IRepoContextFormatter, RepoContextFormatter>();
        services.AddSingleton<IContextPackBuilder, ContextPackBuilder>();
        services.AddSingleton<IRepoGraphBuilder, RepoGraphBuilder>();
        if (configuration is not null)
            services.Configure<RepoGraphOptions>(
                configuration.GetSection("AutonomousAppGeneration:AgentIntegration:ContextPack"));
        else
            services.Configure<RepoGraphOptions>(_ => { });
        services.AddScoped<IAgentIntegrationCoordinator, AgentIntegrationCoordinator>();
        services.AddSingleton<IDiagnosticsBundleService, DiagnosticsBundleService>();
        services.AddSingleton<IArtifactGenerator, ArtifactGenerator>();
        services.AddSingleton<IMcpAdapterRegistry, McpAdapterRegistry>();
        services.AddSingleton<IFlowModeOrchestrator, FlowModeOrchestrator>();
        if (configuration is not null)
            services.Configure<FlowEngineOptions>(configuration.GetSection("AutonomousAppGeneration:FlowEngine"));
        else
            services.Configure<FlowEngineOptions>(_ => { });
        services.AddSingleton<IFlowRegistry, FlowRegistry>();
        services.AddSingleton<IFlowProgressStore, FileFlowProgressStore>();
        services.AddSingleton<IFlowEngine, YamlFlowEngine>();

        // Phase 7.1 вЂ” Agent Fleet index
        if (configuration is not null)
        {
            services.Configure<AgentFleetOptions>(configuration.GetSection(AgentFleetOptions.SectionName));
            var fleetOpts = configuration.GetSection(AgentFleetOptions.SectionName).Get<AgentFleetOptions>();
            if (fleetOpts is not null && !string.IsNullOrWhiteSpace(configuration["AgentRuntime:RunsRoot"]))
                fleetOpts.RunsRoot = configuration["AgentRuntime:RunsRoot"]!;
        }
        else
            services.Configure<AgentFleetOptions>(_ => { });
        services.AddSingleton<IAgentFleetIndexStore, SqliteAgentFleetIndexStore>();
        services.AddSingleton<IAgentFleetEventHub, AgentFleetEventHub>();
        services.AddSingleton<IFleetShipStateStore, FleetShipStateStore>();
        services.AddSingleton<Lazy<IAgentFleetRegistry>>(sp =>
            new Lazy<IAgentFleetRegistry>(() => sp.GetRequiredService<IAgentFleetRegistry>()));
        services.AddSingleton<IFleetShipSyncService, FleetShipSyncService>();
        services.AddSingleton<IFleetSessionSearchService, SqliteFleetSessionSearchService>();
        if (configuration is not null)
            services.Configure<FleetSimilarRunsOptions>(configuration.GetSection(FleetSimilarRunsOptions.SectionName));
        else
            services.Configure<FleetSimilarRunsOptions>(_ => { });
        services.AddSingleton<IFleetSimilarRunsService, FleetSimilarRunsService>();
        services.AddSingleton<IAgentFleetRegistry, AgentFleetRegistry>();
        services.AddSingleton<IRunForkService, RunForkService>();
        services.AddSingleton<IFleetGdprEraseService, FleetGdprEraseService>();
        services.AddSingleton<IFleetGdprExportService, FleetGdprExportService>();
        services.AddSingleton<IFleetRetentionService, FleetRetentionService>();
        if (configuration is not null)
            services.Configure<FleetRetentionOptions>(configuration.GetSection(FleetRetentionOptions.SectionName));
        else
            services.Configure<FleetRetentionOptions>(_ => { });
        services.AddHostedService<FleetRetentionHostedService>();
        services.AddSingleton<ISessionTimelineService, SessionTimelineService>();
        services.AddHostedService<AgentFleetSchemaMigrator>();
        services.AddHostedService<AgentFleetSyncHostedService>();
        services.AddHostedService<AgentFleetStuckRunMonitor>();

        // Phase 7.2 вЂ” Agent Spaces
        if (configuration is not null)
            services.Configure<AgentSpaceOptions>(configuration.GetSection(AgentSpaceOptions.SectionName));
        else
            services.Configure<AgentSpaceOptions>(_ => { });
        services.AddSingleton<ISpaceStore, SqliteSpaceStore>();
        services.AddSingleton<IGitWorktreeService, GitWorktreeService>();
        services.AddSingleton<ISpaceContextBus, FileSpaceContextBus>();
        services.AddSingleton<ISpaceContextFanout, SpaceContextNdjsonFanout>();
        services.AddSingleton<IAgentSpaceService, AgentSpaceService>();
        services.AddSingleton<ISpaceConcurrencyGate, SpaceConcurrencyGate>();
        services.AddSingleton<ISpaceOrchestrator, SpaceOrchestrator>();
        services.AddHostedService<AgentSpaceSchemaMigrator>();
        services.AddHostedService<SpaceWorktreeJanitorHostedService>();

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
        services.AddSingleton<HyperVRuntime>();
        if (configuration is not null)
            services.Configure<IsolatedRuntimeOptions>(configuration.GetSection(IsolatedRuntimeOptions.SectionName));
        else
            services.Configure<IsolatedRuntimeOptions>(_ => { });

        services.AddSingleton<ProcessIsolatedRuntime>();
        services.AddSingleton<RustBackedIsolatedRuntime>();
        services.AddSingleton<IIsolatedRuntime>(sp =>
        {
            var runtimeOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IsolatedRuntimeOptions>>().Value;
            IIsolatedRuntime processRuntime = runtimeOptions.UseRustSandboxExecutor
                ? sp.GetRequiredService<RustBackedIsolatedRuntime>()
                : sp.GetRequiredService<ProcessIsolatedRuntime>();

            return new RuntimeProviderRouter(
                preferredProvider: runtimeProvider ?? "docker",
                allowFallbackToProcess: allowFallbackToProcess,
                docker: sp.GetRequiredService<DockerIsolatedRuntime>(),
                wsl: sp.GetRequiredService<WslIsolatedRuntime>(),
                hyperV: sp.GetRequiredService<HyperVRuntime>(),
                process: processRuntime,
                diagnostics: sp.GetRequiredService<IRuntimeDiagnostics>(),
                logger: sp.GetRequiredService<ILogger<RuntimeProviderRouter>>());
        });
        services.AddSingleton<IWorkspacePool, VmWorkspacePool>();
        services.AddSingleton<IWorkspaceSyncService, FileSystemWorkspaceSyncService>();
        if (configuration is not null)
        {
            services.Configure<ShadowToolchainWarmCacheOptions>(
                configuration.GetSection("AutonomousAppGeneration:WarmCache"));
        }

        services.AddSingleton<IShadowToolchainWarmCache, ShadowToolchainWarmCache>();
        services.AddHostedService<ShadowToolchainWarmCacheHostedService>();

        // Shadow execution = pool + runtime + sync.
        services.AddSingleton<IsolatedShadowExecutionService>();
        services.AddSingleton<IShadowExecutionService>(sp => sp.GetRequiredService<IsolatedShadowExecutionService>());
        services.AddSingleton<IShadowWorkspaceAccessor>(sp => sp.GetRequiredService<IsolatedShadowExecutionService>());
        services.AddObscuraBrowserPlane(configuration, registerObscuraMediatRHandlers);
        services.AddObscuraSessionHostedServices();
        services.AddFastContext(configuration);
        services.AddRunDiffReview();
        services.AddRunHandoff(configuration);
        services.AddAgentBackends(configuration);
        services.Configure<HumanReviewOptions>(configuration.GetSection("AutonomousAppGeneration:HumanReview"));
        if (configuration is not null)
            services.Configure<ShipStageOptions>(configuration.GetSection(ShipStageOptions.SectionName));
        else
            services.Configure<ShipStageOptions>(_ => { });
        services.AddSingleton<IObscuraEvidenceShipGate, ObscuraEvidenceShipGate>();
        services.AddAgentRuntime(configuration);
        services.AddPlatformUtilization();
        services.AddExtensionHost(configuration);
        services.AddGitHubActionsDispatch(configuration);

        // Multi-agent infrastructure for stack-specific generation
        services.AddSingleton<AgentSkillRegistry>(sp =>
        {
            var assemblyLocation = typeof(AgentSkillRegistry).Assembly.Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;
            return new AgentSkillRegistry(
                sp.GetRequiredService<ILogger<AgentSkillRegistry>>(),
                Path.Combine(assemblyDir, "Agents", "Skills"));
        });

        services.AddScoped<IAgentSpawner, AgentSpawner>();
        if (configuration is not null)
        {
            services.Configure<AgentOrchestrationOptions>(
                configuration.GetSection(AgentOrchestrationOptions.SectionName));
            services.Configure<AutonomousBenchmarkModeOptions>(
                configuration.GetSection(AutonomousBenchmarkModeOptions.SectionName));
            services.Configure<AutonomousPlatformUtilizationOptions>(
                configuration.GetSection(AutonomousPlatformUtilizationOptions.SectionName));
        }
        else
        {
            services.Configure<AgentOrchestrationOptions>(_ => { });
        }
        services.AddScoped<AgentOrchestrationFactory>();

        // Reviewer agents (stack-agnostic)
        services.AddScoped<SpecReviewerAgent>(sp => new SpecReviewerAgent(
            Path.Combine(AppContext.BaseDirectory, "Agents", "Skills", "spec-compliance-reviewer", "SKILL.md"),
            sp.GetRequiredService<IAIService>(),
            sp.GetRequiredService<ILogger<SpecReviewerAgent>>()));

        services.AddScoped<CodeQualityReviewerAgent>(sp => new CodeQualityReviewerAgent(
            Path.Combine(AppContext.BaseDirectory, "Agents", "Skills", "code-review", "SKILL.md"),
            sp.GetRequiredService<IAIService>(),
            sp.GetRequiredService<ILogger<CodeQualityReviewerAgent>>()));

        // Legacy agents kept for backward compatibility; they auto-delegate through spawner now
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

