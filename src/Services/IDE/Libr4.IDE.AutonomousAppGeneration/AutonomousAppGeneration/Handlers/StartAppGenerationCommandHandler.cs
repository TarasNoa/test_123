using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.SlashCommands;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.Commands;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Teams;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;
using Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;
using Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.FeatureFlags;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;
using Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Memory;
// alias for the bounded queue interface to avoid name conflicts with the consolidation service.
using IConsolidationQueue = Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Memory.IMemoryConsolidationQueue;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Handlers;

/// <summary>
/// Implements the top-level orchestration loop:
///   Plan -> Generate -> PrepareShadowWorkspace -> Run -> (if errors) Analyze -> Fix -> Re-run
/// until the application succeeds or the iteration budget is exhausted.
/// </summary>
public sealed partial class StartAppGenerationCommandHandler
    : IRequestHandler<StartAppGenerationCommand, AppGenerationResponse>
{
    private readonly IAppPlannerService _planner;
    private readonly ICodeGenerationService _codeGen;
    private readonly IClaudeCodeStyleRepairService _surgicalRepair;
    private readonly IShadowAgentRepairService _agentRepair;
    private readonly IAgentRuntimeIncrementalGenerator? _agentGenerator;
    private readonly AgentRuntimeOptions _agentRuntimeOptions;
    private readonly IShadowExecutionService _shadow;
    private readonly IErrorAnalysisService _errorAnalysis;
    private readonly IAppGenerationRepository _repository;
    private readonly IAutonomousRunControlService _runControl;
    private readonly IAutonomousQualityGateService _qualityGates;
    private readonly AutonomousQualityGateOptions _qualityGateOptions;
    private readonly IAutonomousCodeConsistencyValidator _consistencyValidator;
    private readonly ICheckpointService _checkpoints;
    private readonly ITriggerAdapterRouter _triggerRouter;
    private readonly AutonomousLoopGuardOptions _loopGuardOptions;
    private readonly AutonomousRetryOptions _retryOptions;
    private readonly SecurityReviewGateOptions _securityReviewOptions;
    private readonly AutonomousBenchmarkModeOptions _benchmarkModeOptions;
    private readonly IAgentIntegrationCoordinator _agentIntegration;
    private readonly IFrontendDesignPreplannerService? _frontendDesignPreplanner;
    private readonly IDesignArtifactService? _designArtifacts;
    private readonly IDesignArtifactGenerationBindingService? _designBinding;
    private readonly IReviewGate2Service? _reviewGate2;
    private readonly IPromptContractService? _promptContracts;
    private readonly IFinalReportService? _finalReportService;
    private readonly IReadOnlyList<IRunMiddleware> _middlewares;
    private readonly IReadOnlyList<IAutonomousFinalizationHook> _finalizationHooks;
    private readonly IFeatureFlagService? _featureFlags;
    private readonly IConsolidationQueue? _consolidationQueue;
    private readonly ITeamTemplateResolver? _teamTemplateResolver;
    private readonly ISubagentRoutingService? _subagentRoutingService;
    private readonly ISubagentSelector? _subagentSelector;
    private readonly IGenerationPipelineRunner? _pipelineRunner;
    private readonly IFullGenerationPipelineRunner? _fullPipelineRunner;
    private readonly VerifySubagentOptions _verifyOptions;
    private readonly Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationFactory? _agentOrchestrationFactory;
    private readonly Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationOptions _multiAgentOptions;
    private readonly IMcpToolInvocationService? _mcpTools;
    private readonly IMcpRunHostManager? _mcpRunHost;
    private readonly IFlowEngine? _flowEngine;
    private readonly IRepoGraphBuilder? _repoGraphBuilder;
    private readonly RepoGraphOptions _repoGraphOptions;
    private readonly IUserProfileService? _userProfiles;
    private readonly IWorkspaceTrustRunGate? _workspaceTrust;
    private readonly IAutonomousBatchLlmProfileScope? _batchLlmProfile;
    private readonly IAgentStackRunGate? _agentStackRunGate;
    private readonly IPlatformRunBootstrapService? _platformBootstrap;
    private readonly IPlatformJitCapabilityService? _platformJit;
    private readonly AutonomousPlatformUtilizationOptions _platformUtilizationOptions;
    private readonly ILogger<StartAppGenerationCommandHandler> _logger;

    public StartAppGenerationCommandHandler(
        IAppPlannerService planner,
        ICodeGenerationService codeGen,
        IClaudeCodeStyleRepairService surgicalRepair,
        IShadowAgentRepairService agentRepair,
        IAgentRuntimeIncrementalGenerator? agentGenerator,
        IShadowExecutionService shadow,
        IErrorAnalysisService errorAnalysis,
        IAppGenerationRepository repository,
        IAutonomousRunControlService runControl,
        IAutonomousQualityGateService qualityGates,
        IAutonomousCodeConsistencyValidator consistencyValidator,
        ICheckpointService checkpointService,
        ITriggerAdapterRouter triggerRouter,
        IOptions<AutonomousLoopGuardOptions> loopGuardOptions,
        IOptions<AutonomousRetryOptions> retryOptions,
        IAgentIntegrationCoordinator agentIntegration,
        IFrontendDesignPreplannerService? frontendDesignPreplanner,
        IDesignArtifactService? designArtifacts,
        IDesignArtifactGenerationBindingService? designBinding,
        IReviewGate2Service? reviewGate2,
        IPromptContractService? promptContracts,
        IFinalReportService? finalReportService,
        ITeamTemplateResolver? teamTemplateResolver = null,
        ISubagentRoutingService? subagentRoutingService = null,
        ISubagentSelector? subagentSelector = null,
        IFeatureFlagService? featureFlags = null,
        ILogger<StartAppGenerationCommandHandler>? logger = null,
        IEnumerable<IRunMiddleware>? middlewares = null,
        IEnumerable<IAutonomousFinalizationHook>? finalizationHooks = null,
        IConsolidationQueue? consolidationQueue = null,
        IGenerationPipelineRunner? pipelineRunner = null,
        IFullGenerationPipelineRunner? fullPipelineRunner = null,
        IOptions<VerifySubagentOptions>? verifyOptions = null,
        Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationFactory? agentOrchestrationFactory = null,
        IMcpToolInvocationService? mcpTools = null,
        IMcpRunHostManager? mcpRunHost = null,
        IOptions<Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationOptions>? multiAgentOptions = null,
        IOptions<SecurityReviewGateOptions>? securityReviewOptions = null,
        IOptions<AutonomousBenchmarkModeOptions>? benchmarkModeOptions = null,
        IOptions<AutonomousQualityGateOptions>? qualityGateOptions = null,
        IOptions<AgentRuntimeOptions>? agentRuntimeOptions = null,
        IFlowEngine? flowEngine = null,
        IRepoGraphBuilder? repoGraphBuilder = null,
        IOptions<RepoGraphOptions>? repoGraphOptions = null,
        IUserProfileService? userProfiles = null,
        IWorkspaceTrustRunGate? workspaceTrust = null,
        IAutonomousBatchLlmProfileScope? batchLlmProfile = null,
        IAgentStackRunGate? agentStackRunGate = null,
        IPlatformRunBootstrapService? platformBootstrap = null,
        IPlatformJitCapabilityService? platformJit = null,
        IOptions<AutonomousPlatformUtilizationOptions>? platformUtilizationOptions = null)
    {
        _planner = planner;
        _codeGen = codeGen;
        _surgicalRepair = surgicalRepair;
        _agentRepair = agentRepair;
        _agentGenerator = agentGenerator;
        _agentRuntimeOptions = agentRuntimeOptions?.Value ?? new AgentRuntimeOptions();
        _shadow = shadow;
        _errorAnalysis = errorAnalysis;
        _repository = repository;
        _runControl = runControl;
        _qualityGates = qualityGates;
        _qualityGateOptions = qualityGateOptions?.Value ?? new AutonomousQualityGateOptions();
        _consistencyValidator = consistencyValidator;
        _checkpoints = checkpointService;
        _triggerRouter = triggerRouter;
        _loopGuardOptions = loopGuardOptions.Value;
        _retryOptions = retryOptions.Value;
        _securityReviewOptions = securityReviewOptions?.Value ?? new SecurityReviewGateOptions();
        _benchmarkModeOptions = benchmarkModeOptions?.Value ?? new AutonomousBenchmarkModeOptions();
        _agentIntegration = agentIntegration;
        _frontendDesignPreplanner = frontendDesignPreplanner;
        _designArtifacts = designArtifacts;
        _designBinding = designBinding;
        _reviewGate2 = reviewGate2;
        _promptContracts = promptContracts;
        _finalReportService = finalReportService;
        _teamTemplateResolver = teamTemplateResolver;
        _subagentRoutingService = subagentRoutingService;
        _subagentSelector = subagentSelector;
        _featureFlags = featureFlags;
        _consolidationQueue = consolidationQueue;
        _middlewares = (middlewares ?? Array.Empty<IRunMiddleware>())
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _finalizationHooks = (finalizationHooks ?? Array.Empty<IAutonomousFinalizationHook>())
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _pipelineRunner = pipelineRunner;
        _fullPipelineRunner = fullPipelineRunner;
        _verifyOptions = verifyOptions?.Value ?? new VerifySubagentOptions();
        _agentOrchestrationFactory = agentOrchestrationFactory;
        _multiAgentOptions = multiAgentOptions?.Value ?? new Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationOptions();
        _mcpTools = mcpTools;
        _mcpRunHost = mcpRunHost;
        _flowEngine = flowEngine;
        _repoGraphBuilder = repoGraphBuilder;
        _repoGraphOptions = repoGraphOptions?.Value ?? new RepoGraphOptions();
        _userProfiles = userProfiles;
        _workspaceTrust = workspaceTrust;
        _batchLlmProfile = batchLlmProfile;
        _agentStackRunGate = agentStackRunGate;
        _platformBootstrap = platformBootstrap;
        _platformJit = platformJit;
        _platformUtilizationOptions = platformUtilizationOptions?.Value ?? new AutonomousPlatformUtilizationOptions();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<StartAppGenerationCommandHandler>.Instance;
    }

    private bool IsBenchmarkShortcutActive() =>
        PlatformUtilizationPolicy.IsBenchmarkShortcutPathActive(_benchmarkModeOptions, _platformUtilizationOptions);

    public StartAppGenerationCommandHandler(
        IAppPlannerService planner,
        ICodeGenerationService codeGen,
        IShadowExecutionService shadow,
        IErrorAnalysisService errorAnalysis,
        IAppGenerationRepository repository,
        IAutonomousRunControlService runControl,
        IAutonomousQualityGateService qualityGates,
        IAutonomousCodeConsistencyValidator consistencyValidator,
        IOptions<AutonomousLoopGuardOptions> loopGuardOptions,
        IOptions<AutonomousRetryOptions> retryOptions,
        IAgentIntegrationCoordinator agentIntegration,
        ILogger<StartAppGenerationCommandHandler> logger,
        IClaudeCodeStyleRepairService? surgicalRepair = null,
        IShadowAgentRepairService? agentRepair = null)
        : this(
            planner,
            codeGen,
            surgicalRepair: surgicalRepair ?? new Services.NullClaudeCodeStyleRepairService(),
            agentRepair: agentRepair ?? new AgentRuntime.Services.NullShadowAgentRepairService(),
            agentGenerator: null,
            shadow,
            errorAnalysis,
            repository,
            runControl,
            qualityGates,
            consistencyValidator,
            checkpointService: new InMemoryCheckpointService(),
            triggerRouter: new TriggerAdapterRouter(new[] { new HttpTriggerAdapter() }),
            loopGuardOptions,
            retryOptions,
            agentIntegration,
            frontendDesignPreplanner: null,
            designArtifacts: null,
            designBinding: null,
            reviewGate2: null,
            promptContracts: null,
            finalReportService: null,
            teamTemplateResolver: null,
            subagentRoutingService: null,
            subagentSelector: null,
            featureFlags: null,
            logger: logger,
            middlewares: null,
            finalizationHooks: null,
            mcpTools: null)
    {
    }

    public Task<AppGenerationResponse> Handle(
        StartAppGenerationCommand request, CancellationToken ct)
        => ExecuteCoreAsync(request, ct);
}
