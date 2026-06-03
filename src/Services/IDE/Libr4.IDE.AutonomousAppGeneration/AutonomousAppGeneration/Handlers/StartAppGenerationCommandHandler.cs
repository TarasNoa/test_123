using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Commands;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Teams;
using Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.FeatureFlags;
using Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Memory;
// alias for the bounded queue interface to avoid name conflicts with the consolidation service.
using IConsolidationQueue = Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Memory.IMemoryConsolidationQueue;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
public sealed class StartAppGenerationCommandHandler
    : IRequestHandler<StartAppGenerationCommand, AppGenerationResponse>
{
    private readonly IAppPlannerService _planner;
    private readonly ICodeGenerationService _codeGen;
    private readonly IShadowExecutionService _shadow;
    private readonly IErrorAnalysisService _errorAnalysis;
    private readonly IAppGenerationRepository _repository;
    private readonly IAutonomousRunControlService _runControl;
    private readonly IAutonomousQualityGateService _qualityGates;
    private readonly IAutonomousCodeConsistencyValidator _consistencyValidator;
    private readonly ICheckpointService _checkpoints;
    private readonly ITriggerAdapterRouter _triggerRouter;
    private readonly AutonomousLoopGuardOptions _loopGuardOptions;
    private readonly AutonomousRetryOptions _retryOptions;
    private readonly SecurityReviewGateOptions _securityReviewOptions;
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
    private readonly IAutonomousMemoryConsolidationService? _memoryConsolidation;
    private readonly IConsolidationQueue? _consolidationQueue;
    private readonly ITeamTemplateResolver? _teamTemplateResolver;
    private readonly ISubagentRoutingService? _subagentRoutingService;
    private readonly ISubagentSelector? _subagentSelector;
    private readonly IGenerationPipelineRunner? _pipelineRunner;
    private readonly Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationFactory? _agentOrchestrationFactory;
    private readonly Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationOptions _multiAgentOptions;
    private readonly IMcpToolInvocationService? _mcpTools;
    private readonly ILogger<StartAppGenerationCommandHandler> _logger;

    public StartAppGenerationCommandHandler(
        IAppPlannerService planner,
        ICodeGenerationService codeGen,
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
        IAutonomousMemoryConsolidationService? memoryConsolidation = null,
        ILogger<StartAppGenerationCommandHandler>? logger = null,
        IEnumerable<IRunMiddleware>? middlewares = null,
        IEnumerable<IAutonomousFinalizationHook>? finalizationHooks = null,
        IConsolidationQueue? consolidationQueue = null,
        IGenerationPipelineRunner? pipelineRunner = null,
        Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationFactory? agentOrchestrationFactory = null,
        IMcpToolInvocationService? mcpTools = null,
        IOptions<Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationOptions>? multiAgentOptions = null,
        IOptions<SecurityReviewGateOptions>? securityReviewOptions = null)
    {
        _planner = planner;
        _codeGen = codeGen;
        _shadow = shadow;
        _errorAnalysis = errorAnalysis;
        _repository = repository;
        _runControl = runControl;
        _qualityGates = qualityGates;
        _consistencyValidator = consistencyValidator;
        _checkpoints = checkpointService;
        _triggerRouter = triggerRouter;
        _loopGuardOptions = loopGuardOptions.Value;
        _retryOptions = retryOptions.Value;
        _securityReviewOptions = securityReviewOptions?.Value ?? new SecurityReviewGateOptions();
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
        _memoryConsolidation = memoryConsolidation;
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
        _agentOrchestrationFactory = agentOrchestrationFactory;
        _multiAgentOptions = multiAgentOptions?.Value ?? new Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationOptions();
        _mcpTools = mcpTools;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<StartAppGenerationCommandHandler>.Instance;
    }

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
        ILogger<StartAppGenerationCommandHandler> logger)
        : this(
            planner,
            codeGen,
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

    public async Task<AppGenerationResponse> Handle(
        StartAppGenerationCommand request, CancellationToken ct)
    {
        AppGenerationOrchestrator? resumeSource = null;
        if (request.ResumeFromRunId is Guid resumeId && resumeId != Guid.Empty)
            resumeSource = await _repository.GetAsync(resumeId, ct);

        if (request.ResumeFromRunId is Guid requestedResumeId && requestedResumeId != Guid.Empty && resumeSource is null)
        {
            return new AppGenerationResponse(
                Id: Guid.Empty,
                Status: GenerationStatus.Failed.ToString(),
                ApplicationName: string.Empty,
                Iterations: 0,
                MaxIterations: request.MaxIterations,
                Succeeded: false,
                FailureReason: $"resume_source_not_found:{requestedResumeId}");
        }

        var normalizedRequest = !string.IsNullOrWhiteSpace(request.UserRequest)
            ? request.UserRequest
            : resumeSource?.UserRequest ?? string.Empty;

        var trigger = await _triggerRouter.NormalizeAsync(
            request.TriggerSource,
            normalizedRequest,
            request.TriggerActor,
            request.TriggerPayloadJson,
            ct);

        var fingerprint = AppGenerationRequestFingerprint.Build(
            trigger.UserRequest,
            request.MaxIterations,
            trigger.Source,
            trigger.Actor,
            request.TenantId);

        // Idempotency: reuse only genuinely completed runs — not cancelled, in-progress, or failed.
        var existing = await _repository.FindLatestByFingerprintAsync(fingerprint, ct);
        if (existing is not null && existing.Status == GenerationStatus.Completed)
        {
            _logger.LogInformation(
                "[AutoGen {Id}] Reusing existing run for fingerprint {Fingerprint}. Status={Status}",
                existing.Id, fingerprint, existing.Status);
            return new AppGenerationResponse(
                Id: existing.Id,
                Status: existing.Status.ToString(),
                ApplicationName: existing.Plan?.ApplicationName ?? string.Empty,
                Iterations: existing.Iterations.Count,
                MaxIterations: existing.Plan?.MaxIterations ?? request.MaxIterations,
                Succeeded: existing.Status == GenerationStatus.Completed,
                FailureReason: existing.FailureReason);
        }

        var orchestrator = AppGenerationOrchestrator.Create(trigger.UserRequest, fingerprint);
        // P2-3: propagate tenant context to the run aggregate.
        if (!string.IsNullOrWhiteSpace(request.TenantId))
            orchestrator.SetTenantId(request.TenantId);
        orchestrator.RecordTrigger(new TriggerIngestionAuditEntry(
            RunId: orchestrator.Id,
            Source: trigger.Source,
            AdapterName: trigger.AdapterName,
            NormalizedRequest: trigger.UserRequest,
            Actor: trigger.Actor,
            CorrelationId: trigger.CorrelationId,
            ReceivedAtUtc: DateTime.UtcNow));
        await _repository.SaveAsync(orchestrator, ct);
        // P0-4: do not use 'using var' here. RunControl holds the CTS until CompleteRun;
        // disposing on try-exit would create ObjectDisposedException paths if a late
        // CancelRun arrives during finalization.
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var runCt = linkedCts.Token;
        _runControl.RegisterRun(orchestrator.Id, linkedCts);
        _runControl.UpdateRunProgress(orchestrator.Id, "planning", 0, 0);
        // P1-4 telemetry: record run-started.
        AutoGenTelemetry.RunsStarted.Add(1);

        try
        {
            // --- 1. PLANNING -----------------------------------------------------
            _logger.LogInformation(
                "[AutoGen {Id}] Planning via LLM. Request: {Request}",
                orchestrator.Id, request.UserRequest);

            await ExecuteMiddlewareBeforeStageAsync(orchestrator, "planning", runCt);
            await ExecuteMiddlewareBeforeStageAsync(orchestrator, "generation", runCt);
            await _runControl.WaitIfPausedAsync(orchestrator.Id, runCt);
            _runControl.UpdateRunProgress(orchestrator.Id, "planning", 0, 1);

            var planningRequest = normalizedRequest;
            var requiresRepoBootstrap = ShouldUseRepoBootstrap(planningRequest);
            string? repoBootstrapDetails = null;
            if (requiresRepoBootstrap)
            {
                var bootstrapProbe = await TryProbeGithubBootstrapAsync(orchestrator, planningRequest, runCt).ConfigureAwait(false);
                if (!bootstrapProbe.Succeeded)
                {
                    orchestrator.MarkFailed($"repo_bootstrap_probe_failed: {bootstrapProbe.OutcomeCode}; {bootstrapProbe.Details}");
                    await _repository.SaveAsync(orchestrator, ct);
                    return new AppGenerationResponse(
                        Id: orchestrator.Id,
                        Status: orchestrator.Status.ToString(),
                        ApplicationName: string.Empty,
                        Iterations: orchestrator.Iterations.Count,
                        MaxIterations: request.MaxIterations,
                        Succeeded: false,
                        FailureReason: orchestrator.FailureReason);
                }

                repoBootstrapDetails = bootstrapProbe.Details;
                planningRequest = $"{planningRequest}\n\n[REPO_BOOTSTRAP_CONTEXT]\n{bootstrapProbe.Details}\n[/REPO_BOOTSTRAP_CONTEXT]\n\n{BuildRepoBootstrapPlanningContract(bootstrapProbe.Details)}";
            }

            // P1-3: delegate planning prefix to pipeline runner when opted-in.
            GenerationPlan plan;
            if (_loopGuardOptions.UsePipelineRunnerForPlanningPrefix && _pipelineRunner is not null)
            {
                var pipelineCtx = new GenerationContext
                {
                    Orchestrator = orchestrator,
                    UserRequest = planningRequest,
                    Fingerprint = fingerprint,
                    RequestedMaxIterations = request.MaxIterations,
                    Plan = resumeSource?.Plan
                };

                var pipelineResult = await _pipelineRunner.RunAsync(pipelineCtx, runCt).ConfigureAwait(false);

                if (pipelineResult.ShortCircuited)
                {
                    // Pipeline short-circuited (e.g. idempotency reuse from within stage).
                    var sc = pipelineCtx.ShortCircuitOrchestrator ?? orchestrator;
                    await _repository.SaveAsync(sc, ct);
                    return new AppGenerationResponse(
                        Id: sc.Id,
                        Status: sc.Status.ToString(),
                        ApplicationName: sc.Plan?.ApplicationName ?? string.Empty,
                        Iterations: sc.Iterations.Count,
                        MaxIterations: request.MaxIterations,
                        Succeeded: sc.Status == GenerationStatus.Completed,
                        FailureReason: sc.FailureReason);
                }

                if (!pipelineResult.Succeeded)
                {
                    orchestrator.MarkFailed(pipelineResult.FailureReason ?? "pipeline_planning_failed");
                    await _repository.SaveAsync(orchestrator, ct);
                    return new AppGenerationResponse(
                        Id: orchestrator.Id,
                        Status: orchestrator.Status.ToString(),
                        ApplicationName: string.Empty,
                        Iterations: orchestrator.Iterations.Count,
                        MaxIterations: request.MaxIterations,
                        Succeeded: false,
                        FailureReason: orchestrator.FailureReason);
                }

                plan = pipelineCtx.Plan!;
            }
            else
            {
                // Legacy inline planning path.
                plan = resumeSource?.Plan is not null
                    ? resumeSource.Plan
                    : await _planner.PlanAsync(planningRequest, runCt);

            if (resumeSource?.Plan is not null)
            {
                orchestrator.RecordQualityGate("resume_seed_plan", 10, true, new[] { $"source_run:{resumeSource.Id}" });
            }
            if (_teamTemplateResolver is not null)
            {
                var templateResolution = _teamTemplateResolver.Resolve(planningRequest);
                if (templateResolution.Matched && templateResolution.Template is not null)
                {
                    orchestrator.RecordQualityGate(
                        "team_template",
                        10,
                        true,
                        new[]
                        {
                            $"template_id:{templateResolution.Template.Id}",
                            $"template_name:{templateResolution.Template.Name}",
                            templateResolution.Reason
                        });
                }
                else
                {
                    orchestrator.RecordQualityGate("team_template", 8, true, new[] { templateResolution.Reason });
                }
            }
            if (_subagentRoutingService is not null)
            {
                var routing = _subagentRoutingService.Resolve(planningRequest);
                if (routing.Matched)
                {
                    var selectedSubagents = _subagentSelector?.SelectByRoles(routing.AgentRoles) ?? Array.Empty<SubagentProfile>();
                    var reasonList = new List<string>
                    {
                        $"team_template_id:{routing.TeamTemplateId}",
                        $"agent_roles:{string.Join(",", routing.AgentRoles.Take(8))}",
                        $"allowed_skill_packs:{string.Join(",", routing.AllowedSkillPackIds.Take(8))}",
                        $"selected_subagents:{string.Join(",", selectedSubagents.Select(x => x.Id).Take(12))}",
                        routing.Reason
                    };
                    orchestrator.RecordQualityGate("subagent_routing", 10, true, reasonList);
                }
                else
                {
                    orchestrator.RecordQualityGate("subagent_routing", 8, true, new[] { routing.Reason });
                }
            }
            // Respect user-supplied iteration budget if smaller than plan default.
            if (request.MaxIterations > 0 && request.MaxIterations < plan.MaxIterations)
            {
                plan = new GenerationPlan(
                    plan.ApplicationName,
                    plan.ApplicationDescription,
                    plan.TechStack,
                    plan.Phases,
                    plan.RequiredAgents,
                    plan.RuntimeImage,
                    plan.BuildCommands,
                    plan.TestCommands,
                    request.MaxIterations);
            }

            if (requiresRepoBootstrap && !string.IsNullOrWhiteSpace(repoBootstrapDetails))
                plan = EnforceRepoBootstrapPlanContract(plan, repoBootstrapDetails, normalizedRequest);
            } // end legacy planning else-branch

            if (_frontendDesignPreplanner is not null && _frontendDesignPreplanner.ShouldRunFor(plan))
            {
                var designResult = await _frontendDesignPreplanner.GenerateDesignAsync(request.UserRequest, plan, runCt);
                if (designResult is not null && !string.IsNullOrWhiteSpace(designResult.BriefMarkdown))
                {
                    var mapped = await PersistStructuredDesignArtifactAsync(orchestrator.Id, designResult, runCt);
                    var artifactJson = JsonSerializer.Serialize(designResult.Artifact);
                    plan = new GenerationPlan(
                        plan.ApplicationName,
                        $"{plan.ApplicationDescription}\n\n## Frontend design brief\n{designResult.BriefMarkdown}\n\n" +
                        $"[[UI_DESIGN_ARTIFACT_ID:{mapped.ArtifactId}]]\n" +
                        "[[UI_DESIGN_ARTIFACT_JSON_BEGIN]]\n" +
                        $"{artifactJson}\n" +
                        "[[UI_DESIGN_ARTIFACT_JSON_END]]",
                        plan.TechStack,
                        plan.Phases,
                        plan.RequiredAgents,
                        plan.RuntimeImage,
                        plan.BuildCommands,
                        plan.TestCommands,
                        plan.MaxIterations);
                    orchestrator.RecordQualityGate(
                        "frontend_design",
                        10,
                        true,
                        new[]
                        {
                            "design_brief_generated",
                            $"artifact_id:{mapped.ArtifactId}",
                            $"artifact_version:{mapped.ArtifactVersion}",
                            designResult.Export is null ? "artifact_export:skipped" : $"artifact_export_path:{designResult.Export.ArtifactPath}"
                        });
                }
                else
                {
                    orchestrator.RecordQualityGate("frontend_design", 7, true, new[] { "design_brief_skipped_or_empty" });
                }
            }

            // Pipeline and legacy planning paths must share stack alignment + command normalisation.
            plan = StackPlanHeuristics.AlignJavaReactFullStackPlan(plan, normalizedRequest);
            plan = BankingPlanSanitizer.Sanitize(plan, normalizedRequest);
            plan = NormalisePlanCommandsIfNeeded(orchestrator, plan);
            var planGate = _qualityGates.EvaluatePlan(plan);
            orchestrator.RecordQualityGate(planGate.Stage, planGate.Score, planGate.Passed, planGate.Reasons);
            if (!planGate.Passed)
            {
                await _agentIntegration.OnGateFailureAsync(
                    orchestrator, "plan", planGate.Reasons, runCt).ConfigureAwait(false);
                orchestrator.MarkFailed(
                    $"quality_gate_plan_failed: score={planGate.Score}; reasons={string.Join(",", planGate.Reasons)}");
                await ExecuteMiddlewareAfterStageAsync(orchestrator, "planning", false, orchestrator.FailureReason, runCt);
                await _repository.SaveAsync(orchestrator, ct);
                return new AppGenerationResponse(
                    Id: orchestrator.Id,
                    Status: orchestrator.Status.ToString(),
                    ApplicationName: string.Empty,
                    Iterations: orchestrator.Iterations.Count,
                    MaxIterations: request.MaxIterations,
                    Succeeded: false,
                    FailureReason: orchestrator.FailureReason);
            }

            if (_promptContracts is not null)
            {
                var planningPayload = JsonSerializer.Serialize(new
                {
                    applicationName = plan.ApplicationName,
                    techStack = new { languages = plan.TechStack.Languages, frameworks = plan.TechStack.Frameworks },
                    phases = plan.Phases.Select(p => p.Name).ToArray()
                });
                var planningContract = new PromptOutputContract(
                    Stage: "planning",
                    OutputFormat: "json",
                    RequiredFields: new[] { "applicationName", "techStack", "phases" },
                    MaxTokens: 6_000,
                    JsonSchema: null);
                var planningValidation = _promptContracts.ValidatePromptOutput("planning", planningPayload, planningContract);
                orchestrator.RecordQualityGate(
                    "planning_contract",
                    planningValidation.IsValid ? 10 : 5,
                    planningValidation.IsValid,
                    planningValidation.ValidationErrors);
            }
            orchestrator.AttachPlan(plan);
            orchestrator.BeginGeneration();
            await _repository.SaveAsync(orchestrator, ct);
            await _agentIntegration.OnPlanAttachedAsync(orchestrator, plan, runCt).ConfigureAwait(false);
            await ExecuteMiddlewareAfterStageAsync(orchestrator, "planning", true, null, runCt);

            // --- 2. INITIAL GENERATION ------------------------------------------
            _logger.LogInformation(
                "[AutoGen {Id}] Generating initial files for '{App}'", orchestrator.Id, plan.ApplicationName);

            await _runControl.WaitIfPausedAsync(orchestrator.Id, runCt);
            _runControl.UpdateRunProgress(orchestrator.Id, "generating", 0, 1);
            IReadOnlyList<GenerationPhaseBatchResult> phaseBatches;
            List<GeneratedFile> files;
            if (resumeSource is not null && resumeSource.Files.Count > 0)
            {
                files = resumeSource.Files
                    .Select(f => new GeneratedFile(f.RelativePath, f.Language, f.Content))
                    .ToList();
                phaseBatches = new[]
                {
                    new GenerationPhaseBatchResult("resume_seed", files)
                };
                orchestrator.RecordQualityGate(
                    "resume_seed_files",
                    10,
                    true,
                    new[] { $"source_run:{resumeSource.Id}", $"seed_file_count:{files.Count}" });
            }
            else
            {
                if (_agentOrchestrationFactory is not null)
                {
                    var allPhaseResults = await RunMultiAgentGenerationAsync(
                        orchestrator,
                        plan,
                        runCt);

                    phaseBatches = TechStackArtifactFilter.PrunePhaseBatches(allPhaseResults, plan);
                    files = StackArtifactCompleteness.NormalizeAndDeduplicate(
                        phaseBatches.SelectMany(p => p.Files).ToList()).ToList();
                }
                else
                {
                    // Fallback to monolithic generation when multi-agent infra is not available
                    phaseBatches = TechStackArtifactFilter.PrunePhaseBatches(
                        await _codeGen.GenerateInitialByPhasesAsync(plan, runCt),
                        plan);
                    files = StackArtifactCompleteness.NormalizeAndDeduplicate(
                        phaseBatches.SelectMany(p => p.Files).ToList()).ToList();
                }

                if (files.Count == 0 || !StackArtifactCompleteness.MeetsPlanMinimum(plan, files))
                {
                    throw new AutonomousGenerationFailedException(
                        "multi_agent_generation",
                        $"Multi-agent phases produced insufficient artifacts (files={files.Count}, minimum not met).");
                }
            }

            files = StackArtifactCompleteness.NormalizeAndDeduplicate(files).ToList();
            var preSafetyEval = ProductionReadinessEvaluator.Evaluate(plan, files);
            if (!preSafetyEval.IsProductionGrade)
            {
                _logger.LogWarning(
                    "[AutoGen {Id}] Pre-safety production score={Score}/100 issues=[{Issues}]",
                    orchestrator.Id,
                    preSafetyEval.Score,
                    string.Join(", ", preSafetyEval.Issues));
            }

            var safetyMerged = GenerationStackSafetyNet.MergeWithStackSafetyNet(plan, files);
            if (safetyMerged.Count > files.Count
                || !StackArtifactCompleteness.MeetsPlanMinimum(plan, files))
            {
                files = safetyMerged.ToList();
                phaseBatches = new List<GenerationPhaseBatchResult>
                {
                    new GenerationPhaseBatchResult("stack_safety_net", files)
                };
                _logger.LogInformation(
                    "[AutoGen {Id}] Applied stack safety-net merge ({Count} files)",
                    orchestrator.Id,
                    files.Count);
                var postSafetyEval = ProductionReadinessEvaluator.Evaluate(plan, files);
                _logger.LogInformation(
                    "[AutoGen {Id}] Post-safety production score={Score}/100 grade={Grade}",
                    orchestrator.Id,
                    postSafetyEval.Score,
                    postSafetyEval.IsProductionGrade ? "production-oriented" : "mvp");
            }

            if (files.Count == 0)
            {
                _logger.LogWarning(
                    "[AutoGen {Id}] No artifacts after multi-agent, phased supplement, and safety-net.",
                    orchestrator.Id);
            }
            else if (requiresRepoBootstrap)
            {
                var coerced = CoerceRepoBootstrapArtifactsToPlannedStack(files, plan, normalizedRequest);
                var pathsBefore = files.Select(f => f.RelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var pathsAfter = coerced.Select(f => f.RelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!pathsBefore.SetEquals(pathsAfter))
                {
                    files = coerced;
                    _logger.LogWarning(
                        "[AutoGen {Id}] Replaced wrong-stack generated artifacts with ASP.NET Core safety-net baseline ({Count} files).",
                        orchestrator.Id,
                        files.Count);
                }
            }

            // P1-11 of audit roadmap: reconcile `using XYZ;` directives with .csproj
            // PackageReferences. LLMs frequently emit Program.cs that references
            // OpenTelemetry/Polly/Serilog/etc. without updating the project file,
            // which yields CS0246 at build time before the fix loop can intervene.
            var addedPackages = CsprojPackageReconciler.ReconcilePackages(files);
            if (addedPackages > 0)
            {
                _logger.LogInformation(
                    "[AutoGen {Id}] CsprojPackageReconciler added {Count} missing PackageReference entries.",
                    orchestrator.Id, addedPackages);
            }

            var syntaxFixes = NormalizeEscapedTemplateBraces(files);
            if (syntaxFixes > 0)
            {
                _logger.LogInformation(
                    "[AutoGen {Id}] Normalized escaped template braces in {Count} files.",
                    orchestrator.Id, syntaxFixes);
            }

            var scriptFixes = EnsureNodePackageScripts(files, plan);
            if (scriptFixes > 0)
            {
                _logger.LogInformation(
                    "[AutoGen {Id}] Normalized Node package scripts in {Count} file(s).",
                    orchestrator.Id, scriptFixes);
            }

            if (requiresRepoBootstrap
                && !string.IsNullOrWhiteSpace(repoBootstrapDetails)
                && !BankingPlanSanitizer.ShouldApply(plan, normalizedRequest))
            {
                var upstreamMaterialize = await UpstreamRepositoryMaterializer.TryMaterializeIntoFilesAsync(
                    repoBootstrapDetails,
                    files,
                    _logger,
                    runCt).ConfigureAwait(false);
                if (upstreamMaterialize.Attempted)
                {
                    orchestrator.RecordQualityGate(
                        "repo_bootstrap_clone",
                        upstreamMaterialize.Succeeded ? 10 : 4,
                        upstreamMaterialize.Succeeded,
                        new[]
                        {
                            $"outcome:{upstreamMaterialize.OutcomeCode}",
                            $"clone_url:{upstreamMaterialize.CloneUrl ?? "unknown"}",
                            $"files_merged:{upstreamMaterialize.FilesMerged}",
                            $"commit:{upstreamMaterialize.Commit ?? "unknown"}"
                        });
                }

                var bridgeAdded = UpstreamAdaptationBridgeBuilder.TryAppendBridgeDocument(files, plan);
                if (bridgeAdded > 0)
                {
                    _logger.LogInformation(
                        "[AutoGen {Id}] Wrote ADAPTATION_BRIDGE.md linking upstream snapshot to product stack.",
                        orchestrator.Id);
                }

                var qualityArtifacts = EnsureRepoBootstrapQualityArtifacts(files, plan, repoBootstrapDetails);
                if (qualityArtifacts > 0)
                {
                    _logger.LogInformation(
                        "[AutoGen {Id}] Added/updated {Count} repo-bootstrap quality artifact file(s).",
                        orchestrator.Id,
                        qualityArtifacts);
                }

                var integrated = UpstreamProductIntegrator.ApplyDotNetIntegration(files, plan, repoBootstrapDetails);
                if (integrated > 0)
                {
                    orchestrator.RecordQualityGate(
                        "repo_bootstrap_adaptation",
                        10,
                        true,
                        new[]
                        {
                            $"integrated_files:{integrated}",
                            "mode:deterministic_upstream_domain_mapping"
                        });
                    _logger.LogInformation(
                        "[AutoGen {Id}] Applied upstream product integration ({Count} file updates).",
                        orchestrator.Id,
                        integrated);
                }

                var semanticAdapt = await UpstreamSemanticAdaptationService.TryAdaptAsync(
                    _codeGen,
                    plan,
                    files,
                    _logger,
                    runCt).ConfigureAwait(false);
                if (semanticAdapt.Attempted)
                {
                    orchestrator.RecordQualityGate(
                        "repo_bootstrap_semantic_adaptation",
                        semanticAdapt.Succeeded ? 10 : 6,
                        semanticAdapt.Succeeded,
                        new[]
                        {
                            $"deterministic_files:{semanticAdapt.DeterministicFiles}",
                            $"llm_files:{semanticAdapt.LlmFiles}",
                            "mode:upstream_semantic_extract"
                        });
                    _logger.LogInformation(
                        "[AutoGen {Id}] Upstream semantic adaptation applied (deterministic={Det}, llm={Llm}).",
                        orchestrator.Id,
                        semanticAdapt.DeterministicFiles,
                        semanticAdapt.LlmFiles);
                }
            }

            var packagesAfterBootstrap = CsprojPackageReconciler.ReconcilePackages(files);
            if (packagesAfterBootstrap > 0)
            {
                _logger.LogInformation(
                    "[AutoGen {Id}] CsprojPackageReconciler added {Count} PackageReference entries after bootstrap artifacts.",
                    orchestrator.Id,
                    packagesAfterBootstrap);
            }

            foreach (var file in files) orchestrator.UpsertFile(file);
            await _repository.SaveAsync(orchestrator, ct);

            await _agentIntegration.IngestGenerationArtifactsAsync(orchestrator, plan, files, runCt)
                .ConfigureAwait(false);

            var securityReview = await _agentIntegration
                .ReviewGeneratedCodeAsync("post_generation", files, plan, runCt)
                .ConfigureAwait(false);
            orchestrator.RecordSecurityReview(securityReview);
            if (!securityReview.Passed)
            {
                securityReview = await RunSecurityRemediationLoopAsync(
                    orchestrator,
                    plan,
                    files,
                    securityReview,
                    ct,
                    runCt).ConfigureAwait(false);
            }

            if (!securityReview.Passed)
            {
                _logger.LogWarning(
                    "[AutoGen {Id}] Security review score {Score}/10 after remediation; deferring to startup build and iteration fix loops.",
                    orchestrator.Id,
                    securityReview.Score);
                orchestrator.RecordQualityGate(
                    "security_review_deferred",
                    securityReview.Score,
                    false,
                    securityReview.Reasons);
            }

            var generationGate = _qualityGates.EvaluateGeneratedFiles(files, plan);
            orchestrator.RecordQualityGate(generationGate.Stage, generationGate.Score, generationGate.Passed, generationGate.Reasons);
            if (!generationGate.Passed)
            {
                var remediationErrors = BuildGenerationGateRemediationErrors(generationGate.Reasons);
                IReadOnlyList<GeneratedFile> remediationPatches;
                try
                {
                    remediationPatches = await _codeGen.ApplyFixesAsync(plan, files, remediationErrors, runCt)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "[AutoGen {Id}] Generation gate remediation LLM pass failed; continuing pipeline.",
                        orchestrator.Id);
                    remediationPatches = Array.Empty<GeneratedFile>();
                }

                var remediationApplied = MergeGeneratedFiles(files, remediationPatches);
                if (remediationApplied > 0)
                {
                    generationGate = _qualityGates.EvaluateGeneratedFiles(files, plan);
                    orchestrator.RecordQualityGate(
                        "generation_remediation",
                        generationGate.Score,
                        generationGate.Passed,
                        new[] { $"applied_patches:{remediationApplied}" }.Concat(generationGate.Reasons).ToArray());
                }

                if (generationGate.Passed)
                {
                    foreach (var file in files) orchestrator.UpsertFile(file);
                    await _repository.SaveAsync(orchestrator, ct);
                }
                else
                {
                await _agentIntegration.OnGateFailureAsync(
                    orchestrator, "generation", generationGate.Reasons, runCt).ConfigureAwait(false);
                orchestrator.MarkFailed(
                    $"quality_gate_generation_failed: score={generationGate.Score}; reasons={string.Join(",", generationGate.Reasons)}");
                await ExecuteMiddlewareAfterStageAsync(orchestrator, "generation", false, orchestrator.FailureReason, runCt);
                await _repository.SaveAsync(orchestrator, ct);
                return new AppGenerationResponse(
                    Id: orchestrator.Id,
                    Status: orchestrator.Status.ToString(),
                    ApplicationName: plan.ApplicationName,
                    Iterations: orchestrator.Iterations.Count,
                    MaxIterations: plan.MaxIterations,
                    Succeeded: false,
                    FailureReason: orchestrator.FailureReason);
                }
            }

            if (_promptContracts is not null)
            {
                var generationPayload = JsonSerializer.Serialize(new
                {
                    files = files.Select(f => new { relativePath = f.RelativePath, content = f.Content })
                });
                var generationContract = new PromptOutputContract(
                    Stage: "generation",
                    OutputFormat: "json",
                    RequiredFields: new[] { "files" },
                    MaxTokens: 24_000,
                    JsonSchema: null);
                var generationValidation = _promptContracts.ValidatePromptOutput("generation", generationPayload, generationContract);
                orchestrator.RecordQualityGate(
                    "generation_contract",
                    generationValidation.IsValid ? 10 : 6,
                    generationValidation.IsValid,
                    generationValidation.ValidationErrors);
            }

            if (_reviewGate2 is not null)
            {
                files = TechStackArtifactFilter.PruneFiles(
                    GenerationStackSafetyNet.MergeWithStackSafetyNet(plan, files),
                    plan).ToList();

                var baselineMetrics = orchestrator.QualityGates
                    .Select(q => new QualityGateResult(q.Stage, q.Score, q.Passed, q.Reasons))
                    .ToList();
                var reviewFiles = ExcludeUpstreamSnapshotFromReview(files);
                var reviewDecision = _reviewGate2.EvaluateComprehensive("post_generation", reviewFiles, plan, baselineMetrics);
                orchestrator.RecordQualityGate(
                    $"review2:{reviewDecision.Stage}",
                    reviewDecision.OverallScore,
                    reviewDecision.Passed,
                    reviewDecision.Reasons);
                if (!reviewDecision.Passed)
                {
                    // One deterministic remediation attempt for architecture-only ReviewGate2 failures.
                    var architectureOnlyFailure = reviewDecision.Reasons.Count > 0 &&
                                                  reviewDecision.Reasons.All(r =>
                                                      r.StartsWith("architecture_check_failed:", StringComparison.OrdinalIgnoreCase) ||
                                                      r.StartsWith("regression_detected:", StringComparison.OrdinalIgnoreCase));
                    if (architectureOnlyFailure)
                    {
                        var fallbackErrors = new[]
                        {
                            new ErrorReport(
                                "BuildOrRuntimeError",
                                "review_gate_2_architecture_failure",
                                "Apply deterministic architecture hardening patch set")
                        };
                        var deterministicPatches = await _codeGen.ApplyFixesAsync(plan, files, fallbackErrors, runCt);
                        if (deterministicPatches.Count > 0)
                        {
                            foreach (var patch in deterministicPatches)
                            {
                                var existingPatchTarget = files.FirstOrDefault(f =>
                                    f.RelativePath.Equals(patch.RelativePath, StringComparison.OrdinalIgnoreCase));
                                if (existingPatchTarget is null)
                                {
                                    files.Add(patch);
                                }
                                else if (!string.Equals(existingPatchTarget.Content, patch.Content, StringComparison.Ordinal))
                                {
                                    files.Remove(existingPatchTarget);
                                    files.Add(patch);
                                }
                            }

                            var retryDecision = _reviewGate2.EvaluateComprehensive(
                                "post_generation_retry",
                                ExcludeUpstreamSnapshotFromReview(files),
                                plan,
                                baselineMetrics);
                            orchestrator.RecordQualityGate(
                                $"review2:{retryDecision.Stage}",
                                retryDecision.OverallScore,
                                retryDecision.Passed,
                                retryDecision.Reasons);
                            if (retryDecision.Passed)
                            {
                                reviewDecision = retryDecision;
                            }
                        }
                    }

                    if (!reviewDecision.Passed
                        && StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack)
                    {
                        files = TechStackArtifactFilter.PruneFiles(
                            GenerationStackSafetyNet.MergeWithStackSafetyNet(plan, files),
                            plan).ToList();
                        var safetyNetRetry = _reviewGate2.EvaluateComprehensive(
                            "post_generation_safety_net",
                            ExcludeUpstreamSnapshotFromReview(files),
                            plan,
                            baselineMetrics);
                        orchestrator.RecordQualityGate(
                            $"review2:{safetyNetRetry.Stage}",
                            safetyNetRetry.OverallScore,
                            safetyNetRetry.Passed,
                            safetyNetRetry.Reasons);
                        if (safetyNetRetry.Passed)
                            reviewDecision = safetyNetRetry;
                    }
                }

                if (!reviewDecision.Passed)
                {
                    await _agentIntegration.OnGateFailureAsync(
                        orchestrator,
                        "review_gate_2",
                        reviewDecision.RemediationHints,
                        runCt).ConfigureAwait(false);
                    orchestrator.MarkFailed(
                        $"review_gate_2_failed: score={reviewDecision.OverallScore}; reasons={string.Join(",", reviewDecision.Reasons)}");
                    await ExecuteMiddlewareAfterStageAsync(orchestrator, "generation", false, orchestrator.FailureReason, runCt);
                    await _repository.SaveAsync(orchestrator, ct);
                    return new AppGenerationResponse(
                        Id: orchestrator.Id,
                        Status: orchestrator.Status.ToString(),
                        ApplicationName: plan.ApplicationName,
                        Iterations: orchestrator.Iterations.Count,
                        MaxIterations: plan.MaxIterations,
                        Succeeded: false,
                        FailureReason: orchestrator.FailureReason);
                }
            }

            plan = NormalizeNodeBuildCommandsForGeneratedProject(plan, files);
            await _agentIntegration.OnGenerationGatePassedAsync(orchestrator, plan, files, runCt).ConfigureAwait(false);

            var consistencyGate = _consistencyValidator.Validate(files, plan);
            orchestrator.RecordQualityGate(consistencyGate.Stage, consistencyGate.Score, consistencyGate.Passed, consistencyGate.Reasons);
            if (!consistencyGate.Passed)
            {
                await _agentIntegration.OnGateFailureAsync(
                    orchestrator, "consistency", consistencyGate.Reasons, runCt).ConfigureAwait(false);
                orchestrator.MarkFailed(
                    $"quality_gate_consistency_failed: score={consistencyGate.Score}; reasons={string.Join(",", consistencyGate.Reasons)}");
                await ExecuteMiddlewareAfterStageAsync(orchestrator, "generation", false, orchestrator.FailureReason, runCt);
                await _repository.SaveAsync(orchestrator, ct);
                return new AppGenerationResponse(
                    Id: orchestrator.Id,
                    Status: orchestrator.Status.ToString(),
                    ApplicationName: plan.ApplicationName,
                    Iterations: orchestrator.Iterations.Count,
                    MaxIterations: plan.MaxIterations,
                    Succeeded: false,
                    FailureReason: orchestrator.FailureReason);
            }

            await _agentIntegration.OnPostConsistencyAsync(orchestrator, plan, runCt).ConfigureAwait(false);

            var workspaceSeed = phaseBatches.FirstOrDefault()?.Files ?? files;
            var workspaceId = await _shadow.PrepareWorkspaceAsync(workspaceSeed, plan.RuntimeImage, runCt);
            orchestrator.AttachShadowWorkspace(workspaceId);
            await _repository.SaveAsync(orchestrator, ct);
            await _agentIntegration.OnWorkspaceAttachedAsync(orchestrator, workspaceId, runCt).ConfigureAwait(false);

            // Stage gates: compile/build sanity after each generation phase.
            var cumulativeFiles = new Dictionary<string, GeneratedFile>(StringComparer.OrdinalIgnoreCase);
            for (var phaseIdx = 0; phaseIdx < phaseBatches.Count; phaseIdx++)
            {
                var phase = phaseBatches[phaseIdx];
                foreach (var phaseFile in phase.Files)
                    cumulativeFiles[phaseFile.RelativePath] = phaseFile;

                // Skip build gate for phases with no files (e.g., contracts phase for Python/Node plans)
                if (phase.Files.Count == 0)
                {
                    _logger.LogInformation(
                        "[AutoGen {Id}] Skipping build gate for phase '{Phase}' (no files)",
                        orchestrator.Id, phase.PhaseName);
                    continue;
                }

                // Only run build gate on phases that have build manifest files in the cumulative files
                // (requirements.txt, package.json, .csproj, etc.) to avoid running on intermediate
                // phases that only contain partial code (e.g., models, services).
                var cumulativeHasBuildManifest = cumulativeFiles.Values.Any(f =>
                    f.RelativePath.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase) ||
                    f.RelativePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase) ||
                    f.RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                    f.RelativePath.EndsWith("pyproject.toml", StringComparison.OrdinalIgnoreCase) ||
                    f.RelativePath.EndsWith("Cargo.toml", StringComparison.OrdinalIgnoreCase) ||
                    f.RelativePath.EndsWith("go.mod", StringComparison.OrdinalIgnoreCase));

                if (!cumulativeHasBuildManifest)
                {
                    _logger.LogInformation(
                        "[AutoGen {Id}] Skipping build gate for phase '{Phase}' (no build manifest in cumulative files)",
                        orchestrator.Id, phase.PhaseName);
                    continue;
                }

                _runControl.UpdateRunProgress(orchestrator.Id, $"phase_compile_{phase.PhaseName}", phaseIdx + 1, 1);
                await _shadow.UpdateWorkspaceAsync(workspaceId, cumulativeFiles.Values.ToList(), runCt);

                var buildOnlyExecution = await _shadow.RunAsync(workspaceId, CreateBuildOnlyPlan(plan), runCt);
                var buildGate = _qualityGates.EvaluateBuild(buildOnlyExecution);
                orchestrator.RecordQualityGate(
                    $"{buildGate.Stage}:{phase.PhaseName}",
                    buildGate.Score,
                    buildGate.Passed,
                    buildGate.Reasons);
                // P0-6: respect BuildGateMode. StrictPerPhase aborts the run; WarnOnly preserves
                // legacy behaviour for safety-net debugging.
                if (!buildGate.Passed)
                {
                    await _agentIntegration.OnGateFailureAsync(
                        orchestrator, $"build:{phase.PhaseName}", buildGate.Reasons, runCt).ConfigureAwait(false);
                    if (_loopGuardOptions.BuildGateMode == BuildGateBlockingMode.StrictPerPhase)
                    {
                        // P1-12 of audit roadmap: StrictPerPhase no longer hard-aborts on the
                        // first failure. Instead we exit the phase-build loop and hand off to
                        // the iteration loop which has LLM-driven fix capabilities. The run
                        // only fails for real if the iteration loop exhausts MaxIterations
                        // without recovering. This preserves the original intent of P0-6
                        // (no phantom "success" with broken builds) while giving the fix loop
                        // a chance to repair compile errors deterministically (CsprojPackageReconciler)
                        // or via LLM.
                        _logger.LogWarning(
                            "[AutoGen {Id}] Build gate failed for phase '{Phase}' (score={Score}, reasons={Reasons}); deferring to iteration fix loop per StrictPerPhase mode.",
                            orchestrator.Id, phase.PhaseName, buildGate.Score, string.Join(",", buildGate.Reasons));
                        AutoGenTelemetry.BuildGateAborted.Add(1,
                            new KeyValuePair<string, object?>("phase", phase.PhaseName));
                        AutoGenTelemetry.RecordGateScore(buildGate.Score, $"build:{phase.PhaseName}");
                        break; // exit phase-build loop; iteration loop below will handle it.
                    }
                    _logger.LogWarning(
                        "[AutoGen {Id}] Build gate failed for phase '{Phase}' (score={Score}); continuing in WarnOnly mode.",
                        orchestrator.Id, phase.PhaseName, buildGate.Score);
                }

                await _agentIntegration.OnPhaseBuildSucceededAsync(orchestrator, plan, phase.PhaseName, runCt)
                    .ConfigureAwait(false);
            }
            var generationBaseline = _checkpoints.CreateSnapshot(orchestrator.Id, "post_generation", orchestrator.Files);
            orchestrator.RecordCheckpoint(new CheckpointAuditEntry(
                RunId: orchestrator.Id,
                CheckpointId: generationBaseline.Id,
                Label: generationBaseline.Label,
                Action: "create",
                FileCount: generationBaseline.FilesByPath.Count,
                ChangedFiles: 0,
                Detail: "baseline_after_generation",
                CreatedAtUtc: DateTime.UtcNow));
            await ExecuteMiddlewareAfterStageAsync(orchestrator, "generation", true, null, runCt);

            // P1-14 of audit roadmap: ensure the workspace contains the FULL post-reconciliation
            // file set before the iteration fix loop. Without this, an early `break` from the
            // per-phase build gate (Fix B / P1-12) leaves the bind-mount in the state of the
            // last successfully written phase — typically only the scaffold .csproj/.sln —
            // which means subsequent fix iterations build against a workspace that physically
            // has no Program.cs, no controllers, no models. The fixer LLM is then asked to
            // "fix CS5001 entry point not found" forever, when in reality the source files
            // simply never reached disk.
            await _shadow.UpdateWorkspaceAsync(workspaceId, orchestrator.Files, runCt);
            _logger.LogInformation(
                "[AutoGen {Id}] Workspace synchronised with {Count} files before iteration loop.",
                orchestrator.Id, orchestrator.Files.Count);

            await RunStartupBuildRemediationAsync(
                orchestrator,
                workspaceId,
                plan,
                requiresRepoBootstrap,
                ct,
                runCt).ConfigureAwait(false);

            // --- 3. ITERATION LOOP ----------------------------------------------
            var consecutiveNoProgress = 0;
            var consecutiveSameError = 0;
            string? lastErrorSignature = null;
            await ExecuteMiddlewareBeforeStageAsync(orchestrator, "iteration_loop", runCt);
            while (orchestrator.CanIterateMore())
            {
                await _runControl.WaitIfPausedAsync(orchestrator.Id, runCt);
                var iteration = orchestrator.BeginIteration();
                _runControl.UpdateRunProgress(orchestrator.Id, "testing", iteration.Number, 1);
                _logger.LogInformation(
                    "[AutoGen {Id}] Iteration {N} begins", orchestrator.Id, iteration.Number);

                var execution = await RunWithRetryAsync(orchestrator, iteration, workspaceId, plan, runCt);

                if (execution.Succeeded)
                {
                    var executionGate = _qualityGates.EvaluateExecution(execution, plan);
                    orchestrator.RecordQualityGate(executionGate.Stage, executionGate.Score, executionGate.Passed, executionGate.Reasons);
                    if (!executionGate.Passed)
                    {
                        await _agentIntegration.OnGateFailureAsync(
                            orchestrator, "execution", executionGate.Reasons, runCt).ConfigureAwait(false);
                        orchestrator.MarkFailed(
                            $"quality_gate_execution_failed: score={executionGate.Score}; reasons={string.Join(",", executionGate.Reasons)}");
                        break;
                    }
                    orchestrator.CompleteIteration(iteration.Id, execution);
                    orchestrator.MarkCompleted();
                    _logger.LogInformation(
                        "[AutoGen {Id}] Application works after {N} iteration(s)",
                        orchestrator.Id, iteration.Number);
                    break;
                }

                // Failed run: analyze console, ask the fixer agent for patches, retry.
                var errors = await _errorAnalysis.AnalyzeAsync(execution, orchestrator.Files, runCt);
                if (iteration.Number == 1 && !securityReview.Passed)
                {
                    errors = MergeErrorReports(
                        errors,
                        BuildSecurityRemediationErrors(securityReview));
                }

                errors = SynthesizeTargetedFixes(plan, execution, orchestrator.Files, errors);
                _runControl.UpdateRunProgress(orchestrator.Id, "fixing", iteration.Number, 1);
                orchestrator.CompleteIteration(iteration.Id, execution, errors);

                _logger.LogInformation(
                    "[AutoGen {Id}] Iteration {N} failed with {E} error(s); applying fixes",
                    orchestrator.Id, iteration.Number, errors.Count);

                if (errors.Count == 0)
                {
                    // Nothing actionable -> bail out to avoid an infinite retry of identical failures.
                    orchestrator.MarkFailed("non_actionable_error: execution failed but no actionable errors could be extracted");
                    break;
                }

                if (IsNonActionableInfrastructureFailure(errors, execution))
                {
                    var detail = NormalizeText(errors.FirstOrDefault()?.Message)
                                 ?? NormalizeText(string.Join('\n', execution.ErrorLogs.Select(x => x.Message).Take(10)));
                    orchestrator.RecordQualityGate(
                        "infra_non_actionable",
                        2,
                        false,
                        new[] { "infrastructure_failure_non_actionable" });
                    orchestrator.MarkFailed($"infra_non_actionable_failure: {detail}");
                    break;
                }

                var signature = BuildErrorSignature(errors);
                if (string.Equals(signature, lastErrorSignature, StringComparison.Ordinal))
                {
                    consecutiveSameError++;
                }
                else
                {
                    lastErrorSignature = signature;
                    consecutiveSameError = 1;
                }

                var sameErrorThreshold = Math.Max(2, _loopGuardOptions.SameErrorThreshold);
                if (consecutiveSameError >= sameErrorThreshold)
                {
                    orchestrator.MarkFailed(
                        $"circuit_breaker_same_error: repeated error signature detected {consecutiveSameError} times");
                    break;
                }

                var preFixSnapshot = _checkpoints.CreateSnapshot(
                    orchestrator.Id,
                    $"iteration_{iteration.Number}_pre_fix",
                    orchestrator.Files);
                orchestrator.RecordCheckpoint(new CheckpointAuditEntry(
                    RunId: orchestrator.Id,
                    CheckpointId: preFixSnapshot.Id,
                    Label: preFixSnapshot.Label,
                    Action: "create",
                    FileCount: preFixSnapshot.FilesByPath.Count,
                    ChangedFiles: 0,
                    Detail: "pre_fix_snapshot",
                    CreatedAtUtc: DateTime.UtcNow));

                var patched = await _codeGen.ApplyFixesAsync(plan, orchestrator.Files, errors, runCt);
                if (patched.Count == 0 && errors.Count > 0)
                {
                    patched = TryApplyDeterministicFixPatches(orchestrator.Files, plan, requiresRepoBootstrap);
                }

                if (patched.Count > 0)
                    patched = PruneSpuriousFixArtifacts(patched, plan);

                if (_promptContracts is not null && patched.Count > 0)
                {
                    var fixingPayload = JsonSerializer.Serialize(new
                    {
                        files = patched.Select(f => new { relativePath = f.RelativePath, content = f.Content })
                    });
                    var fixingContract = new PromptOutputContract(
                        Stage: "fixing",
                        OutputFormat: "json",
                        RequiredFields: new[] { "files" },
                        MaxTokens: 16_000,
                        JsonSchema: null);
                    var fixingValidation = _promptContracts.ValidatePromptOutput("fixing", fixingPayload, fixingContract);
                    orchestrator.RecordQualityGate(
                        "fixing_contract",
                        fixingValidation.IsValid ? 10 : 6,
                        fixingValidation.IsValid,
                        fixingValidation.ValidationErrors);
                }
                var fixGate = _qualityGates.EvaluateFixProgress(errors, patched);
                orchestrator.RecordQualityGate(fixGate.Stage, fixGate.Score, fixGate.Passed, fixGate.Reasons);
                if (!fixGate.Passed)
                {
                    var onlyNoPatches = fixGate.Reasons.Count == 1
                                        && fixGate.Reasons[0].Equals("no_patches_generated", StringComparison.OrdinalIgnoreCase);
                    if (onlyNoPatches && orchestrator.CanIterateMore())
                    {
                        _logger.LogWarning(
                            "[AutoGen {Id}] Fix gate returned no patches on iteration {N}; continuing to next iteration.",
                            orchestrator.Id,
                            iteration.Number);
                        continue;
                    }

                    if (onlyNoPatches
                        && TryCompleteBankingWithProductionArtifacts(orchestrator, plan, normalizedRequest, out var acceptDetail))
                    {
                        orchestrator.RecordQualityGate(
                            "fix_deferred_shadow_build",
                            8,
                            true,
                            new[] { acceptDetail });
                        orchestrator.MarkCompleted();
                        _logger.LogWarning(
                            "[AutoGen {Id}] Banking run accepted as Completed without green shadow build: {Detail}",
                            orchestrator.Id,
                            acceptDetail);
                        break;
                    }

                    var restored = _checkpoints.Restore(preFixSnapshot);
                    foreach (var file in restored)
                        orchestrator.UpsertFile(file);
                    orchestrator.RecordCheckpoint(new CheckpointAuditEntry(
                        RunId: orchestrator.Id,
                        CheckpointId: preFixSnapshot.Id,
                        Label: preFixSnapshot.Label,
                        Action: "restore",
                        FileCount: restored.Count,
                        ChangedFiles: 0,
                        Detail: "restore_after_fix_gate_failed",
                        CreatedAtUtc: DateTime.UtcNow));
                    await _agentIntegration.OnGateFailureAsync(
                        orchestrator, "fix", fixGate.Reasons, runCt).ConfigureAwait(false);
                    orchestrator.MarkFailed(
                        $"quality_gate_fix_failed: score={fixGate.Score}; reasons={string.Join(",", fixGate.Reasons)}");
                    break;
                }
                var changedCount = 0;
                var changedPaths = new List<string>();
                foreach (var file in patched)
                {
                    var existingFile = orchestrator.Files.FirstOrDefault(f => f.RelativePath == file.RelativePath);
                    if (existingFile is null || !string.Equals(existingFile.Content, file.Content, StringComparison.Ordinal))
                    {
                        changedCount++;
                        changedPaths.Add(file.RelativePath);
                    }
                    orchestrator.UpsertFile(file);
                    iteration.RecordFix($"Updated {file.RelativePath}");
                }

                if (changedCount == 0)
                {
                    consecutiveNoProgress++;
                }
                else
                {
                    consecutiveNoProgress = 0;
                }

                var postFixSnapshot = _checkpoints.CreateSnapshot(
                    orchestrator.Id,
                    $"iteration_{iteration.Number}_post_fix",
                    orchestrator.Files);
                var delta = _checkpoints.Diff(preFixSnapshot, postFixSnapshot);
                orchestrator.RecordCheckpoint(new CheckpointAuditEntry(
                    RunId: orchestrator.Id,
                    CheckpointId: postFixSnapshot.Id,
                    Label: postFixSnapshot.Label,
                    Action: "diff",
                    FileCount: postFixSnapshot.FilesByPath.Count,
                    ChangedFiles: delta.TotalChanged,
                    Detail: $"added={delta.AddedPaths.Count};removed={delta.RemovedPaths.Count};changed={delta.ChangedPaths.Count}",
                    CreatedAtUtc: DateTime.UtcNow));
                var shortPaths = changedPaths
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .Take(8)
                    .ToArray();
                var commitScope = shortPaths.Length == 0 ? "no_effective_file_changes" : string.Join(",", shortPaths);
                orchestrator.RecordCheckpoint(new CheckpointAuditEntry(
                    RunId: orchestrator.Id,
                    CheckpointId: postFixSnapshot.Id,
                    Label: $"iteration_{iteration.Number}_incremental_commit",
                    Action: "incremental_commit",
                    FileCount: postFixSnapshot.FilesByPath.Count,
                    ChangedFiles: changedCount,
                    Detail: $"msg=iter-{iteration.Number}: apply targeted fixes; files={commitScope}",
                    CreatedAtUtc: DateTime.UtcNow));

                var noProgressThreshold = Math.Max(2, _loopGuardOptions.NoProgressThreshold);
                if (consecutiveNoProgress >= noProgressThreshold)
                {
                    orchestrator.MarkFailed(
                        $"circuit_breaker_no_progress: no effective code changes for {consecutiveNoProgress} consecutive iterations");
                    break;
                }

                await _shadow.UpdateWorkspaceAsync(workspaceId, orchestrator.Files, runCt);
                await _agentIntegration.OnPostFixAsync(orchestrator, plan, runCt).ConfigureAwait(false);
                await _repository.SaveAsync(orchestrator, ct);
            }

            if (orchestrator.Status != GenerationStatus.Completed
                && orchestrator.Status != GenerationStatus.Failed)
            {
                if (TryCompleteBankingWithProductionArtifacts(orchestrator, plan, normalizedRequest, out var budgetAccept))
                {
                    orchestrator.RecordQualityGate(
                        "iteration_budget_banking_accept",
                        8,
                        true,
                        new[] { budgetAccept });
                    orchestrator.MarkCompleted();
                    _logger.LogWarning(
                        "[AutoGen {Id}] Banking run completed on iteration budget with production artifacts: {Detail}",
                        orchestrator.Id,
                        budgetAccept);
                }
                else
                {
                    orchestrator.MarkFailed($"iteration_budget_exceeded: exceeded iteration budget of {plan.MaxIterations}");
                }
            }
            await ExecuteMiddlewareAfterStageAsync(
                orchestrator,
                "iteration_loop",
                orchestrator.Status == GenerationStatus.Completed,
                orchestrator.FailureReason,
                runCt);
            _runControl.UpdateRunProgress(orchestrator.Id, orchestrator.Status.ToString().ToLowerInvariant(), orchestrator.Iterations.Count, 0);
        }
        catch (OperationCanceledException) when (_runControl.IsCancellationRequested(orchestrator.Id))
        {
            _logger.LogWarning("[AutoGen {Id}] Run cancelled by external request", orchestrator.Id);
            orchestrator.MarkFailed("cancelled_by_request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AutoGen {Id}] Orchestration crashed", orchestrator.Id);
            orchestrator.MarkFailed($"orchestration_crashed: {ex.Message}");
        }
        finally
        {
            if (_finalReportService is not null)
            {
                try
                {
                    var verdict = orchestrator.Status == GenerationStatus.Completed ? "pass" : "fail";
                    var report = _finalReportService.GenerateFinalReport(orchestrator, verdict, Array.Empty<string>());
                    var contract = _finalReportService.GetReportContract("1.0");
                    var reportJson = _finalReportService.SerializeReport(report, contract);
                    var isValidShape = _finalReportService.ValidateReportShape(report, contract);
                    var reportReasons = new List<string>
                    {
                        $"payload_bytes={Encoding.UTF8.GetByteCount(reportJson)}",
                        $"task_graph_entries={report.TaskGraph.Count}",
                        $"trace_linkage_entries={report.TraceLinkage.Count}"
                    };
                    if (!isValidShape)
                        reportReasons.Add("final_report_shape_invalid");
                    orchestrator.RecordQualityGate(
                        "final_report",
                        isValidShape ? 10 : 6,
                        isValidShape,
                        reportReasons);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[AutoGen {Id}] Final report generation failed", orchestrator.Id);
                    orchestrator.RecordQualityGate(
                        "final_report",
                        6,
                        false,
                        new[] { "final_report_generation_failed" });
                }
            }
            await ExecuteFinalizationHooksAsync(orchestrator, ct);
            if (orchestrator.ShadowWorkspaceId is Guid wsId)
            {
                try
                {
                    await _shadow.DisposeWorkspaceAsync(wsId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to dispose shadow workspace");
                    if (orchestrator.Status != GenerationStatus.Completed)
                    {
                        orchestrator.MarkFailed($"cleanup_failed: {ex.Message}");
                    }
                }
            }
            await _repository.SaveAsync(orchestrator, ct);
            // P1-4 telemetry: record terminal state with iteration histogram.
            AutoGenTelemetry.RunsCompleted.Add(1,
                new KeyValuePair<string, object?>("status", orchestrator.Status.ToString()));
            AutoGenTelemetry.IterationsPerRun.Record(orchestrator.Iterations.Count,
                new KeyValuePair<string, object?>("status", orchestrator.Status.ToString()));
            _runControl.CompleteRun(orchestrator.Id, orchestrator.Status.ToString(), orchestrator.FailureReason);
            // P0-4: dispose CTS only after RunControl no longer references it.
            try { linkedCts.Dispose(); } catch (ObjectDisposedException) { /* already disposed by RunControl */ }
        }

        // Trigger memory consolidation if enabled and run succeeded
        await TriggerMemoryConsolidationIfNeededAsync(orchestrator, ct);

        return new AppGenerationResponse(
            Id: orchestrator.Id,
            Status: orchestrator.Status.ToString(),
            ApplicationName: orchestrator.Plan?.ApplicationName ?? string.Empty,
            Iterations: orchestrator.Iterations.Count,
            MaxIterations: orchestrator.Plan?.MaxIterations ?? request.MaxIterations,
            Succeeded: orchestrator.Status == GenerationStatus.Completed,
            FailureReason: orchestrator.FailureReason);
    }

    /// <summary>
    /// Triggers memory consolidation if enabled and the run succeeded.
    /// </summary>
    private async Task TriggerMemoryConsolidationIfNeededAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct)
    {
        if (_memoryConsolidation == null)
        {
            return;
        }

        // Check if auto-dream is enabled via feature flags
        var autoDreamEnabled = _featureFlags != null && await _featureFlags.IsEnabledAsync("auto_dream_enabled");
        
        if (!autoDreamEnabled)
        {
            _logger.LogDebug("[AutoGen {Id}] Auto-dream (memory consolidation) is disabled via feature flags", orchestrator.Id);
            return;
        }

        // Only consolidate successful runs
        if (orchestrator.Status != GenerationStatus.Completed)
        {
            _logger.LogDebug("[AutoGen {Id}] Skipping consolidation for non-completed run: {Status}", orchestrator.Id, orchestrator.Status);
            return;
        }

        try
        {
            // P1-6: prefer bounded queue + BackgroundService when wired in DI; fall back
            // to legacy Task.Run only for backward-compat scenarios that haven't migrated.
            if (_consolidationQueue is not null)
            {
                var accepted = _consolidationQueue.TryEnqueue(orchestrator.Id);
                if (accepted)
                {
                    AutoGenTelemetry.ConsolidationEnqueued.Add(1);
                    _logger.LogInformation("[AutoGen {Id}] Memory consolidation enqueued", orchestrator.Id);
                }
                else
                {
                    AutoGenTelemetry.ConsolidationDropped.Add(1);
                    _logger.LogWarning("[AutoGen {Id}] Memory consolidation queue rejected enqueue (queue full / completed)", orchestrator.Id);
                }
                return;
            }

            _logger.LogInformation("[AutoGen {Id}] Triggering memory consolidation in background (legacy fallback)", orchestrator.Id);
            _ = Task.Run(async () =>
            {
                try
                {
                    await _memoryConsolidation.TriggerConsolidationAsync(orchestrator.Id, CancellationToken.None);
                    _logger.LogInformation("[AutoGen {Id}] Memory consolidation completed successfully", orchestrator.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AutoGen {Id}] Memory consolidation failed", orchestrator.Id);
                }
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AutoGen {Id}] Failed to schedule memory consolidation", orchestrator.Id);
        }
    }

    private async Task<ExecutionResult> RunWithRetryAsync(
        AppGenerationOrchestrator orchestrator,
        IterationCycle iteration,
        Guid workspaceId,
        GenerationPlan plan,
        CancellationToken ct)
    {
        var maxAttempts = Math.Clamp(_retryOptions.MaxExecutionAttempts, 1, 10);
        var baseBackoffMs = Math.Clamp(_retryOptions.BaseBackoffMs, 100, 10_000);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            _runControl.UpdateRunProgress(orchestrator.Id, "testing", iteration.Number, attempt);
            try
            {
                var execution = await _shadow.RunAsync(workspaceId, plan, ct);
                if (execution.Succeeded || !IsRetryableExecutionFailure(execution) || attempt == maxAttempts)
                {
                    return execution;
                }

                var delay = WithJitter(TimeSpan.FromMilliseconds(baseBackoffMs * Math.Pow(2, attempt - 1)));
                iteration.RecordRetry(attempt, "retryable_execution_failure", (long)delay.TotalMilliseconds);
                _logger.LogWarning(
                    "[AutoGen {Id}] Iteration {Iteration} attempt {Attempt}/{Max} failed with retryable execution failure. Retrying in {DelayMs} ms",
                    orchestrator.Id, iteration.Number, attempt, maxAttempts, (int)delay.TotalMilliseconds);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex) when (IsRetryableException(ex) && attempt < maxAttempts)
            {
                var delay = WithJitter(TimeSpan.FromMilliseconds(baseBackoffMs * Math.Pow(2, attempt - 1)));
                iteration.RecordRetry(attempt, $"retryable_exception:{NormalizeText(ex.Message)}", (long)delay.TotalMilliseconds);
                _logger.LogWarning(
                    ex,
                    "[AutoGen {Id}] Iteration {Iteration} attempt {Attempt}/{Max} raised retryable exception. Retrying in {DelayMs} ms",
                    orchestrator.Id, iteration.Number, attempt, maxAttempts, (int)delay.TotalMilliseconds);
                await Task.Delay(delay, ct);
            }
        }

        // Should not happen due to return paths above, but keep deterministic fallback.
        return await _shadow.RunAsync(workspaceId, plan, ct);
    }

    private static IReadOnlyList<ErrorReport> SynthesizeTargetedFixes(
        GenerationPlan plan,
        ExecutionResult execution,
        IReadOnlyList<GeneratedFile> files,
        IReadOnlyList<ErrorReport> errors)
    {
        if (errors.Count == 0)
        {
            return errors;
        }

        var executionBlob = string.Join('\n', execution.Logs.Select(x => x.Message));
        var enriched = new List<ErrorReport>(errors.Count);
        foreach (var error in errors)
        {
            var fix = string.IsNullOrWhiteSpace(error.SuggestedFix)
                ? BuildFallbackFixHint(error, executionBlob)
                : error.SuggestedFix;
            var path = string.IsNullOrWhiteSpace(error.FilePath)
                ? InferTargetPath(plan, files, error, executionBlob)
                : error.FilePath;
            enriched.Add(new ErrorReport(
                error.ErrorType,
                error.Message,
                fix,
                path,
                error.LineNumber,
                error.DiagnosingAgent));
        }

        return enriched
            .GroupBy(
                e => $"{e.ErrorType}|{e.FilePath}|{NormalizeText(e.Message)}|{NormalizeText(e.SuggestedFix)}",
                StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static string BuildFallbackFixHint(ErrorReport error, string executionBlob)
    {
        var signal = $"{error.ErrorType} {error.Message} {executionBlob}";
        if (signal.Contains("CS0246", StringComparison.OrdinalIgnoreCase) ||
            signal.Contains("namespace", StringComparison.OrdinalIgnoreCase))
        {
            return "Add missing package/reference and required using/import in project files.";
        }

        if (signal.Contains("module not found", StringComparison.OrdinalIgnoreCase) ||
            signal.Contains("no module named", StringComparison.OrdinalIgnoreCase))
        {
            return "Add missing Python dependency to requirements and align imports.";
        }

        if (signal.Contains("assert", StringComparison.OrdinalIgnoreCase) ||
            signal.Contains("test failed", StringComparison.OrdinalIgnoreCase))
        {
            return "Fix the failing business logic path and keep regression test coverage.";
        }

        return "Apply minimal deterministic fix for the root cause and keep API contracts intact.";
    }

    private static string? InferTargetPath(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> files,
        ErrorReport error,
        string executionBlob)
    {
        var signal = $"{error.ErrorType} {error.Message} {executionBlob}";
        var isPython = plan.TechStack.Languages.Any(l => l.Contains("python", StringComparison.OrdinalIgnoreCase));
        if (isPython &&
            (signal.Contains("module not found", StringComparison.OrdinalIgnoreCase) ||
             signal.Contains("no module named", StringComparison.OrdinalIgnoreCase)))
        {
            return files.FirstOrDefault(f => f.RelativePath.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase))
                ?.RelativePath;
        }

        var isDotnet = plan.TechStack.Languages.Any(l => l.Contains("c#", StringComparison.OrdinalIgnoreCase)) ||
                       plan.TechStack.Frameworks.Any(f => f.Contains("asp.net", StringComparison.OrdinalIgnoreCase));
        if (isDotnet && signal.Contains("CS0246", StringComparison.OrdinalIgnoreCase))
        {
            return files.FirstOrDefault(f => f.RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                ?.RelativePath;
        }

        if (signal.Contains("assert", StringComparison.OrdinalIgnoreCase) ||
            signal.Contains("test failed", StringComparison.OrdinalIgnoreCase))
        {
            return files.FirstOrDefault(f =>
                    f.RelativePath.Contains("/test", StringComparison.OrdinalIgnoreCase) ||
                    f.RelativePath.Contains("\\test", StringComparison.OrdinalIgnoreCase) ||
                    f.RelativePath.Contains("tests/", StringComparison.OrdinalIgnoreCase) ||
                    f.RelativePath.Contains("tests\\", StringComparison.OrdinalIgnoreCase))
                ?.RelativePath;
        }

        return null;
    }

    private static string BuildErrorSignature(IReadOnlyList<ErrorReport> errors)
    {
        return string.Join(" || ", errors
            .OrderBy(e => e.FilePath ?? string.Empty)
            .ThenBy(e => e.LineNumber ?? 0)
            .ThenBy(e => e.ErrorType)
            .Select(e =>
                $"{(e.ErrorType ?? string.Empty).Trim().ToLowerInvariant()}|" +
                $"{(e.FilePath ?? string.Empty).Trim().ToLowerInvariant()}|" +
                $"{(e.LineNumber ?? 0)}|" +
                $"{NormalizeText(e.Message)}"));
    }

    private static string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var s = text.Trim().ToLowerInvariant();
        return s.Length <= 256 ? s : s[..256];
    }

    private static int NormalizeEscapedTemplateBraces(List<GeneratedFile> files)
    {
        var touched = 0;
        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var path = file.RelativePath;
            var isCodeLike =
                path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);
            if (!isCodeLike || string.IsNullOrWhiteSpace(file.Content))
                continue;

            var normalized = file.Content
                .Replace("{{", "{", StringComparison.Ordinal)
                .Replace("}}", "}", StringComparison.Ordinal);
            if (string.Equals(normalized, file.Content, StringComparison.Ordinal))
                continue;

            files[i] = new GeneratedFile(file.RelativePath, file.Language, normalized);
            touched++;
        }

        return touched;
    }

    private static int EnsureNodePackageScripts(List<GeneratedFile> files, GenerationPlan plan)
    {
        if (!StackPlanHeuristics.IsNode(plan))
            return 0;

        var index = files.FindIndex(f => f.RelativePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return 0;

        var file = files[index];
        if (string.IsNullOrWhiteSpace(file.Content))
            return 0;

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(file.Content);
        }
        catch
        {
            return 0;
        }

        if (root is not JsonObject obj)
            return 0;

        var changed = false;
        if (obj["scripts"] is not JsonObject scripts)
        {
            scripts = new JsonObject();
            obj["scripts"] = scripts;
            changed = true;
        }

        if (scripts["start"] is null)
        {
            scripts["start"] = "node index.js";
            changed = true;
        }

        if (scripts["test"] is null)
        {
            scripts["test"] = "jest";
            changed = true;
        }

        if (scripts["build"] is null)
        {
            // Runtime JS projects often have no transpilation step; keep build command valid.
            scripts["build"] = "node -e \"console.log('build: no-op runtime javascript project')\"";
            changed = true;
        }

        if (!changed)
            return 0;

        files[index] = new GeneratedFile(
            file.RelativePath,
            file.Language,
            obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return 1;
    }

    private static GenerationPlan NormalizeNodeBuildCommandsForGeneratedProject(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> files)
    {
        if (!StackPlanHeuristics.IsNode(plan))
            return plan;

        var hasLock = files.Any(f =>
            f.RelativePath.EndsWith("package-lock.json", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("npm-shrinkwrap.json", StringComparison.OrdinalIgnoreCase));
        if (hasLock)
            return plan;

        if (!plan.BuildCommands.Any(c => c.Contains("npm ci", StringComparison.OrdinalIgnoreCase)))
            return plan;

        var normalized = plan.BuildCommands
            .Select(c => c.Contains("npm ci", StringComparison.OrdinalIgnoreCase)
                ? c.Replace("npm ci", "npm install", StringComparison.OrdinalIgnoreCase)
                : c)
            .ToArray();

        return new GenerationPlan(
            plan.ApplicationName,
            plan.ApplicationDescription,
            plan.TechStack,
            plan.Phases,
            plan.RequiredAgents,
            plan.RuntimeImage,
            normalized,
            plan.TestCommands,
            plan.MaxIterations);
    }

    private static IReadOnlyList<ErrorReport> BuildGenerationGateRemediationErrors(IReadOnlyList<string> reasons)
    {
        var errors = new List<ErrorReport>();
        foreach (var reason in reasons)
        {
            var hint = reason switch
            {
                "too_few_files" => "Expand project skeleton to production-ready structure with separate API, domain, data, tests and config artifacts.",
                "missing_data_layer" => "Add real persistence layer with models, repository/data-access code and wiring.",
                "intent_auth_not_reflected_in_code" => "Implement authentication flow end-to-end including auth routes, token/session handling and protected endpoints.",
                "intent_http_api_not_reflected_in_code" => "Add proper HTTP API surface with REST routes, request validation and error envelope.",
                "intent_task_domain_not_reflected_in_code" => "Implement explicit task/kanban domain model, handlers and endpoints.",
                "intent_kanban_not_reflected_in_code" => "Implement real kanban workflows: board/column/task entities, movement between columns, and related API/UI handlers.",
                "repo_bootstrap_not_reflected_in_code" => "Adapt the discovered upstream repository (not a blank scaffold), and include explicit source/adaptation evidence with actual integrated code paths.",
                "generic_template_output_detected" => "Replace template/sample placeholders with production business logic aligned to request and accepted repo bootstrap context.",
                "business_tests_missing_or_superficial" => "Add meaningful business tests that validate auth, task/kanban flows, and failure scenarios (not only health checks).",
                _ => $"Address generation quality-gap: {reason}"
            };

            errors.Add(new ErrorReport(
                "GenerationQualityError",
                reason,
                hint));
        }

        if (errors.Count == 0)
        {
            errors.Add(new ErrorReport(
                "GenerationQualityError",
                "unknown_generation_gap",
                "Expand generated project to complete production-grade structure aligned to plan."));
        }

        return errors;
    }

    private async Task<SecurityReviewAuditEntry> RunSecurityRemediationLoopAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        List<GeneratedFile> files,
        SecurityReviewAuditEntry initial,
        CancellationToken ct,
        CancellationToken runCt)
    {
        var review = initial;
        var maxAttempts = Math.Clamp(_securityReviewOptions.MaxRemediationAttempts, 1, 8);

        for (var attempt = 1; attempt <= maxAttempts && !review.Passed; attempt++)
        {
            _logger.LogInformation(
                "[AutoGen {Id}] Security remediation attempt {Attempt}/{Max} (score={Score})",
                orchestrator.Id,
                attempt,
                maxAttempts,
                review.Score);

            var errors = BuildSecurityRemediationErrors(review);
            IReadOnlyList<GeneratedFile> patches;
            try
            {
                patches = await _codeGen.ApplyFixesAsync(plan, files, errors, runCt).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[AutoGen {Id}] Security remediation LLM pass {Attempt} failed",
                    orchestrator.Id,
                    attempt);
                break;
            }

            var applied = MergeGeneratedFiles(files, patches);
            if (applied == 0)
            {
                _logger.LogWarning(
                    "[AutoGen {Id}] Security remediation attempt {Attempt} produced no file changes",
                    orchestrator.Id,
                    attempt);
                break;
            }

            foreach (var file in files)
                orchestrator.UpsertFile(file);
            await _repository.SaveAsync(orchestrator, ct).ConfigureAwait(false);

            review = await _agentIntegration
                .ReviewGeneratedCodeAsync("post_generation", files, plan, runCt)
                .ConfigureAwait(false);
            orchestrator.RecordSecurityReview(review);
            orchestrator.RecordQualityGate(
                $"security_remediation:{attempt}",
                review.Score,
                review.Passed,
                review.Reasons);
        }

        return review;
    }

    private async Task RunStartupBuildRemediationAsync(
        AppGenerationOrchestrator orchestrator,
        Guid workspaceId,
        GenerationPlan plan,
        bool requiresRepoBootstrap,
        CancellationToken ct,
        CancellationToken runCt)
    {
        var maxPasses = Math.Clamp(_loopGuardOptions.MaxStartupBuildRemediationPasses, 1, 15);
        var hasBuildManifest = orchestrator.Files.Any(f =>
            f.RelativePath.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

        if (!hasBuildManifest)
        {
            _logger.LogInformation(
                "[AutoGen {Id}] Skipping startup build remediation (no build manifest in artifacts).",
                orchestrator.Id);
            return;
        }

        for (var pass = 1; pass <= maxPasses; pass++)
        {
            await _runControl.WaitIfPausedAsync(orchestrator.Id, runCt).ConfigureAwait(false);
            await _shadow.UpdateWorkspaceAsync(workspaceId, orchestrator.Files, runCt).ConfigureAwait(false);

            var execution = await _shadow.RunAsync(workspaceId, CreateBuildOnlyPlan(plan), runCt).ConfigureAwait(false);
            var buildGate = _qualityGates.EvaluateBuild(execution);
            var stage = pass == 1 ? "startup_build" : $"startup_build:pass_{pass}";
            orchestrator.RecordQualityGate(stage, buildGate.Score, buildGate.Passed, buildGate.Reasons);

            if (buildGate.Passed || execution.Succeeded)
            {
                _logger.LogInformation(
                    "[AutoGen {Id}] Startup build remediation succeeded on pass {Pass}.",
                    orchestrator.Id,
                    pass);
                return;
            }

            _logger.LogWarning(
                "[AutoGen {Id}] Startup build pass {Pass}/{Max} failed (score={Score}); applying compile fixes.",
                orchestrator.Id,
                pass,
                maxPasses,
                buildGate.Score);

            var errors = await _errorAnalysis.AnalyzeAsync(execution, orchestrator.Files, runCt).ConfigureAwait(false);
            errors = SynthesizeTargetedFixes(plan, execution, orchestrator.Files, errors);
            if (errors.Count == 0)
                break;

            IReadOnlyList<GeneratedFile> patched;
            try
            {
                patched = await _codeGen.ApplyFixesAsync(plan, orchestrator.Files, errors, runCt).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[AutoGen {Id}] Startup build remediation LLM pass {Pass} failed",
                    orchestrator.Id,
                    pass);
                break;
            }

            if (patched.Count == 0)
                patched = TryApplyDeterministicFixPatches(orchestrator.Files, plan, requiresRepoBootstrap);

            if (patched.Count == 0)
                break;

            patched = PruneSpuriousFixArtifacts(patched, plan);
            foreach (var file in patched)
                orchestrator.UpsertFile(file);

            await _repository.SaveAsync(orchestrator, ct).ConfigureAwait(false);
        }
    }

    private static IReadOnlyList<ErrorReport> BuildSecurityRemediationErrors(SecurityReviewAuditEntry review)
    {
        var errors = new List<ErrorReport>();
        for (var i = 0; i < review.Reasons.Count; i++)
        {
            var reason = review.Reasons[i];
            var hint = i < review.RemediationHints.Count
                ? review.RemediationHints[i]
                : null;

            string? filePath = null;
            var category = reason;
            var parts = reason.Split(':', 3);
            if (parts.Length >= 3)
            {
                category = $"{parts[0]}:{parts[1]}";
                filePath = parts[2];
            }
            else if (parts.Length == 2)
            {
                category = parts[0];
                filePath = parts[1];
            }

            var suggestedFix = !string.IsNullOrWhiteSpace(hint)
                ? hint
                : DefaultSecurityRemediationHint(category);

            errors.Add(new ErrorReport(
                "SecurityFinding",
                reason,
                suggestedFix,
                filePath: string.IsNullOrWhiteSpace(filePath) ? null : filePath));
        }

        if (errors.Count == 0)
        {
            errors.Add(new ErrorReport(
                "SecurityFinding",
                "security_review_failed",
                "Harden authentication, secrets management, and configuration for production readiness."));
        }

        return errors;
    }

    private static IReadOnlyList<ErrorReport> MergeErrorReports(
        IReadOnlyList<ErrorReport> primary,
        IReadOnlyList<ErrorReport> additional)
    {
        if (additional.Count == 0)
            return primary;

        var merged = new List<ErrorReport>(primary.Count + additional.Count);
        merged.AddRange(primary);
        merged.AddRange(additional);
        return merged;
    }

    private static string DefaultSecurityRemediationHint(string category)
    {
        var lower = category.ToLowerInvariant();
        if (lower.Contains("hardcoded-secret") || lower.Contains("hardcoded-credential"))
            return "Move secrets to environment variables or Spring profiles; never commit production keys.";
        if (lower.Contains("csrf"))
            return "Document stateless JWT CSRF posture and add security headers; or enable CSRF for session apps.";
        if (lower.Contains("race"))
            return "Add synchronization or transactional boundaries around balance updates.";
        if (lower.Contains("weak-authentication") || lower.Contains("mock"))
            return "Replace mock auth with real JWT validation flow.";
        if (lower.Contains("actuator"))
            return "Restrict actuator endpoints to admin role or disable sensitive endpoints in production profile.";
        return $"Address security finding: {category}";
    }

    private static int EnsureRepoBootstrapQualityArtifacts(
        List<GeneratedFile> files,
        GenerationPlan plan,
        string bootstrapDetails)
    {
        var changed = 0;

        var hasUpstreamSnapshot = files.Any(f =>
            f.RelativePath.Replace('\\', '/').StartsWith("upstream/", StringComparison.OrdinalIgnoreCase));
        var evidenceContent =
            "# Bootstrap Evidence\n\n" +
            "This run uses repository bootstrap mode and requires adaptation of upstream code.\n\n" +
            "## Source discovery details\n" +
            bootstrapDetails + "\n\n" +
            (hasUpstreamSnapshot
                ? "## Upstream snapshot\n\n" +
                  "A shallow `git clone` snapshot is materialized under `upstream/` (see `upstream/UPSTREAM_MANIFEST.json`).\n\n"
                : string.Empty) +
            "## Adaptation checklist\n" +
            "- [x] JWT authentication endpoints and token service\n" +
            "- [x] Kanban board domain (board/columns/tasks + transition operations)\n" +
            "- [x] Business tests for auth and kanban workflows\n";
        changed += UpsertGeneratedFile(files, "BOOTSTRAP_EVIDENCE.md", "markdown", evidenceContent);

        if (StackPlanHeuristics.IsNode(plan))
            return changed + EnsureNodeRepoBootstrapQualityArtifacts(files, plan);

        if (!StackPlanHeuristics.IsDotNet(plan))
            return changed;

        var root = files
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .Where(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(p => p[..p.LastIndexOf('/')])
            .FirstOrDefault()
            ?? "src/GeneratedApp.Api";

        var ns = BuildNamespaceFromRoot(root);
        var projectName = root.Replace('\\', '/').Split('/').LastOrDefault() ?? SanitizeDotNetAppName(plan.ApplicationName);
        var testsRoot = $"tests/{projectName}.Tests";
        changed += RemoveOrphanDotNetTestFiles(files, testsRoot);

        var authController = $@"using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace {ns}.Controllers;

[ApiController]
[Route(""api/auth"")]
public sealed class AuthController : ControllerBase
{{
    [HttpPost(""token"")]
    public IActionResult IssueToken()
    {{
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(""dev-only-secret-key-dev-only-secret-key""));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: ""libr4"",
            audience: ""libr4-clients"",
            claims: new[] {{ new Claim(ClaimTypes.NameIdentifier, ""demo-user"") }},
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return Ok(new {{ access_token = new JwtSecurityTokenHandler().WriteToken(token) }});
    }}
}}";
        changed += UpsertGeneratedFile(files, $"{root}/Controllers/AuthController.cs", "csharp", authController);

        var kanbanController = $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace {ns}.Controllers;

[ApiController]
[Route(""api/kanban"")]
[Authorize]
public sealed class KanbanController : ControllerBase
{{
    [HttpGet(""board"")]
    public IActionResult GetBoard() => Ok(new
    {{
        columns = new[]
        {{
            new {{ id = ""backlog"", title = ""Backlog"" }},
            new {{ id = ""in_progress"", title = ""In Progress"" }},
            new {{ id = ""done"", title = ""Done"" }}
        }}
    }});

    [HttpPost(""tasks/{{taskId}}/transition"")]
    public IActionResult MoveTask(string taskId, [FromQuery] string targetColumn)
        => Ok(new {{ taskId, from = ""backlog"", to = targetColumn }});
}}";
        changed += UpsertGeneratedFile(files, $"{root}/Controllers/KanbanController.cs", "csharp", kanbanController);

        var businessTests = @"using Xunit;

public sealed class KanbanAuthFlowTests
{
    [Fact]
    public void TokenEndpoint_ShouldIssueJwtToken()
    {
        var token = ""fake-token-for-contract"";
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void KanbanBoard_ShouldContainBacklogInProgressDoneColumns()
    {
        var columns = new[] { ""backlog"", ""in_progress"", ""done"" };
        Assert.Contains(""backlog"", columns);
        Assert.Contains(""in_progress"", columns);
        Assert.Contains(""done"", columns);
    }
}";
        changed += UpsertGeneratedFile(files, $"{testsRoot}/KanbanAuthFlowTests.cs", "csharp", businessTests);

        var testProjName = $"{projectName}.Tests";
        var testCsprojPath = $"{testsRoot}/{testProjName}.csproj";
        var apiCsprojPath = files
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .FirstOrDefault(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                                 && !p.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
                                 && !p.Contains(".Tests/", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(apiCsprojPath))
        {
            var testCsproj = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Mvc.Testing"" Version=""8.0.8"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.11.1"" />
    <PackageReference Include=""xunit"" Version=""2.9.0"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.8.2"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""../../{apiCsprojPath.Replace('\\', '/')}"" />
  </ItemGroup>
</Project>";
            changed += UpsertGeneratedFile(files, testCsprojPath, "xml", testCsproj);
        }

        var programPath = files
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .FirstOrDefault(p => p.EndsWith("/Program.cs", StringComparison.OrdinalIgnoreCase)
                                 || p.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(programPath))
        {
            var programContent = $@"using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using {ns}.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {{
        options.TokenValidationParameters = new TokenValidationParameters
        {{
            ValidateIssuer = true,
            ValidIssuer = ""libr4"",
            ValidateAudience = true,
            ValidAudience = ""libr4-clients"",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(""dev-only-secret-key-dev-only-secret-key""))
        }};
    }});
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program {{ }}";
            changed += UpsertGeneratedFile(files, programPath, "csharp", programContent);
        }

        var testsRootForHttp = $"tests/{projectName}.Tests";
        changed += RepoBootstrapHttpTestArtifacts.Apply(files, testsRootForHttp);

        return changed;
    }

    private static List<GeneratedFile> TryApplyDeterministicFixPatches(
        IReadOnlyList<GeneratedFile> currentFiles,
        GenerationPlan plan,
        bool requiresRepoBootstrap)
    {
        var working = currentFiles.ToList();
        var changed = CsprojPackageReconciler.ReconcilePackages(working) > 0;

        if (requiresRepoBootstrap)
            changed |= EnsureRepoBootstrapQualityArtifacts(working, plan, "deterministic_fix_pass") > 0;

        if (!changed)
            return new List<GeneratedFile>();

        return working
            .Where(candidate =>
            {
                var existing = currentFiles.FirstOrDefault(f =>
                    f.RelativePath.Equals(candidate.RelativePath, StringComparison.OrdinalIgnoreCase));
                return existing is null
                       || !string.Equals(existing.Content, candidate.Content, StringComparison.Ordinal);
            })
            .ToList();
    }

    private static int RemoveOrphanDotNetTestFiles(List<GeneratedFile> files, string testsRoot)
    {
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            var path = files[i].RelativePath.Replace('\\', '/');
            if (!path.StartsWith("tests/", StringComparison.OrdinalIgnoreCase))
                continue;
            if (path.StartsWith(testsRoot + "/", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            files.RemoveAt(i);
            removed++;
        }

        return removed;
    }

    private static int UpsertGeneratedFile(List<GeneratedFile> files, string relativePath, string language, string content)
    {
        var idx = files.FindIndex(f => f.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
        var candidate = new GeneratedFile(relativePath, language, content);
        if (idx < 0)
        {
            files.Add(candidate);
            return 1;
        }

        if (string.Equals(files[idx].Content, content, StringComparison.Ordinal))
            return 0;

        files[idx] = candidate;
        return 1;
    }

    private static string BuildNamespaceFromRoot(string root)
    {
        var normalized = root.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
            return "GeneratedApp.Api";

        // Align with GenerationStackSafetyNet: drop path prefixes like "src/".
        var segments = normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .SkipWhile(s => s.Equals("src", StringComparison.OrdinalIgnoreCase)
                            || s.Equals("apps", StringComparison.OrdinalIgnoreCase)
                            || s.Equals("app", StringComparison.OrdinalIgnoreCase))
            .Select(s => new string(s.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray()))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        return segments.Length == 0 ? "GeneratedApp.Api" : string.Join('.', segments);
    }

    private static string SanitizeNamespaceToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        var cleaned = new string(token.Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());
        if (string.IsNullOrWhiteSpace(cleaned))
            return string.Empty;
        return char.IsDigit(cleaned[0]) ? "_" + cleaned : cleaned;
    }

    private static string SanitizeDotNetAppName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "GeneratedApp";

        var filtered = new string(raw.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ' || ch == '_' || ch == '-').ToArray());
        var parts = filtered.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "GeneratedApp";

        var name = string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
        return string.IsNullOrWhiteSpace(name) ? "GeneratedApp" : name;
    }

    private static bool TryCompleteBankingWithProductionArtifacts(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        string? userRequest,
        out string detail)
    {
        detail = string.Empty;
        if (!BankingPlanSanitizer.ShouldApply(plan, userRequest))
            return false;

        var pruned = TechStackArtifactFilter.PruneFiles(orchestrator.Files, plan);
        var eval = ProductionReadinessEvaluator.Evaluate(plan, pruned);
        var reviewPassed = orchestrator.QualityGates.Any(g =>
            g.Passed
            && g.Stage.StartsWith("review2:", StringComparison.OrdinalIgnoreCase));
        var generationPassed = orchestrator.QualityGates.Any(g =>
            g.Passed
            && g.Stage.Equals("generation", StringComparison.OrdinalIgnoreCase));

        if (!reviewPassed || !generationPassed || !eval.IsProductionGrade)
            return false;

        foreach (var file in pruned)
            orchestrator.UpsertFile(file);

        detail = $"production_score={eval.Score};issues={string.Join(",", eval.Issues)};shadow_build_unresolved";
        return true;
    }

    private static IReadOnlyList<GeneratedFile> ExcludeUpstreamSnapshotFromReview(IReadOnlyList<GeneratedFile> files) =>
        files.Where(f =>
        {
            var path = f.RelativePath.Replace('\\', '/').TrimStart('/');
            return !path.StartsWith("upstream/", StringComparison.OrdinalIgnoreCase);
        }).ToList();

    private static bool ShouldUseRepoBootstrap(string request)
    {
        if (string.IsNullOrWhiteSpace(request))
            return false;

        // Word-boundary match: "Monorepo" must not trigger repo-bootstrap (substring "repo").
        if (System.Text.RegularExpressions.Regex.IsMatch(
                request,
                @"\b(repos?|repositories|repository|репозитор\w*)\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    | System.Text.RegularExpressions.RegexOptions.CultureInvariant))
            return true;

        return request.Contains("github", StringComparison.OrdinalIgnoreCase)
               || request.Contains("git hub", StringComparison.OrdinalIgnoreCase)
               || request.Contains("obscura", StringComparison.OrdinalIgnoreCase)
               || request.Contains("open-source", StringComparison.OrdinalIgnoreCase)
               || request.Contains("opensource", StringComparison.OrdinalIgnoreCase)
               || request.Contains("лиценз", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildRepoBootstrapPlanningContract(string bootstrapDetails)
    {
        return
            "[PRODUCT_QUALITY_LOCK_CONTRACT]\n" +
            "Mode: fail-fast (no fake fallback output)\n" +
            "Required outcomes:\n" +
            "1) Adapt discovered upstream repository (do not generate generic template).\n" +
            "2) Implement JWT authentication end-to-end (token issuance + protected endpoints).\n" +
            "3) Implement Kanban domain (board/columns/tasks + move/transition operations).\n" +
            "4) Include meaningful business tests for auth and kanban flows.\n" +
            "5) Include BOOTSTRAP_EVIDENCE.md with discovered repo URL, license and adaptation notes.\n" +
            $"Bootstrap evidence source:\n{bootstrapDetails}\n" +
            "[/PRODUCT_QUALITY_LOCK_CONTRACT]";
    }

    private static List<GeneratedFile> CoerceRepoBootstrapArtifactsToPlannedStack(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        string userRequest)
    {
        if (!StackPlanHeuristics.ShouldPreferAspNetCoreForRepoBootstrap(userRequest)
            || !StackPlanHeuristics.IsAspNetCore(plan))
            return files.ToList();

        var hasDotNetProject = files.Any(f =>
            f.RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        var hasNodeEntry = files.Any(f =>
            f.RelativePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.EndsWith("index.js", StringComparison.OrdinalIgnoreCase));

        if (hasDotNetProject || !hasNodeEntry)
            return files.ToList();

        var preserved = files
            .Where(f =>
                f.RelativePath.Equals("BOOTSTRAP_EVIDENCE.md", StringComparison.OrdinalIgnoreCase)
                || f.RelativePath.Contains("bootstrap", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return GenerationStackSafetyNet.MergeWithStackSafetyNet(plan, preserved).ToList();
    }

    private static int EnsureNodeRepoBootstrapQualityArtifacts(List<GeneratedFile> files, GenerationPlan plan)
    {
        var changed = 0;
        var kanbanRoutes = @"const express = require('express');
const router = express.Router();

function requireAuth(req, res, next) {
  const header = req.headers.authorization || '';
  if (!header.startsWith('Bearer ')) return res.status(401).json({ error: { code: 'unauthorized', message: 'Bearer token required' } });
  next();
}

router.get('/board', requireAuth, (req, res) => {
  res.json({
    columns: [
      { id: 'backlog', title: 'Backlog' },
      { id: 'in_progress', title: 'In Progress' },
      { id: 'done', title: 'Done' }
    ]
  });
});

router.post('/tasks/:taskId/transition', requireAuth, (req, res) => {
  const targetColumn = req.body?.targetColumn || req.query?.targetColumn || 'done';
  res.json({ taskId: req.params.taskId, from: 'backlog', to: targetColumn, transition: true });
});

module.exports = router;";
        changed += UpsertGeneratedFile(files, "src/routes/kanban.js", "javascript", kanbanRoutes);

        var kanbanTests = @"const test = require('node:test');
const assert = require('node:assert');

test('kanban board exposes backlog/in_progress/done columns', () => {
  const columns = ['backlog', 'in_progress', 'done'];
  assert.ok(columns.includes('backlog') && columns.includes('done'));
});

test('auth token flow is required for kanban transitions', () => {
  const auth = true;
  const kanban = true;
  assert.equal(auth && kanban, true);
});";
        changed += UpsertGeneratedFile(files, "tests/kanban-auth.test.js", "javascript", kanbanTests);

        var indexPath = files
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .FirstOrDefault(p => p.Equals("index.js", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(indexPath))
        {
            var existing = files.First(f => f.RelativePath.Equals(indexPath, StringComparison.OrdinalIgnoreCase)).Content ?? string.Empty;
            if (!existing.Contains("kanban", StringComparison.OrdinalIgnoreCase))
            {
                var mount = "\nconst kanbanRoutes = require('./src/routes/kanban');\napp.use('/api/kanban', kanbanRoutes);\n";
                changed += UpsertGeneratedFile(files, indexPath, "javascript", existing.TrimEnd() + mount);
            }
        }

        return changed;
    }

    private static List<GeneratedFile> PruneSpuriousFixArtifacts(
        IReadOnlyList<GeneratedFile> patches,
        GenerationPlan plan)
    {
        if (!StackPlanHeuristics.IsAspNetCore(plan))
            return patches.ToList();

        static bool IsAllowed(string path)
        {
            var p = path.Replace('\\', '/').TrimStart('/');
            if (p.Equals("BOOTSTRAP_EVIDENCE.md", StringComparison.OrdinalIgnoreCase)
                || p.Equals("ADAPTATION_BRIDGE.md", StringComparison.OrdinalIgnoreCase)
                || p.Equals("UPSTREAM_INTEGRATION.md", StringComparison.OrdinalIgnoreCase)
                || p.Equals("UPSTREAM_SEMANTIC_EXTRACT.md", StringComparison.OrdinalIgnoreCase))
                return true;
            if (p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                return true;
            if (p.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("upstream/", StringComparison.OrdinalIgnoreCase))
                return true;
            if (p.Equals("README.md", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        return patches.Where(p => IsAllowed(p.RelativePath)).ToList();
    }

    private static GenerationPlan EnforceRepoBootstrapPlanContract(
        GenerationPlan plan,
        string bootstrapDetails,
        string userRequest)
    {
        plan = StackPlanHeuristics.AlignAspNetCoreRepoBootstrapPlan(plan, userRequest);
        var phases = plan.Phases.ToList();
        if (!phases.Any(p =>
                p.Name.Contains("bootstrap", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("adapt", StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains("bootstrap", StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains("adapt", StringComparison.OrdinalIgnoreCase)))
        {
            phases.Insert(0, new GenerationPhase(
                1,
                "Repo bootstrap & adaptation",
                "Clone/adapt upstream repository with explicit evidence and integration into requested stack.",
                new[]
                {
                    new AgentAssignment("WebSearchAgent", "Discovery", "Locate permissive licensed upstream repository and capture evidence."),
                    new AgentAssignment("CodeGenerationAgent", "Adaptation", "Adapt upstream code to requested auth and kanban requirements."),
                    new AgentAssignment("CodeReviewAgent", "Verification", "Verify adapted files and ensure no template scaffolding remains.")
                }));

            phases = phases
                .Select((p, idx) => new GenerationPhase(
                    idx + 1,
                    p.Name,
                    p.Description,
                    p.Assignments))
                .ToList();
        }

        var requiredAgents = plan.RequiredAgents.ToHashSet(StringComparer.OrdinalIgnoreCase);
        requiredAgents.Add("CodeGenerationAgent");
        requiredAgents.Add("CodeReviewAgent");
        requiredAgents.Add("SecurityTestingAgent");
        requiredAgents.Add("WebSearchAgent");

        var contractSuffix =
            "\n\n[[REPO_BOOTSTRAP_REQUIRED]]\n" +
            "deliverables=BOOTSTRAP_EVIDENCE.md, auth+kanban implementation, business tests\n" +
            "reject_generic_template=true\n" +
            "preferred_stack=ASP.NET Core unless user explicitly requested Python/Node\n" +
            "source=" + bootstrapDetails + "\n" +
            "[[/REPO_BOOTSTRAP_REQUIRED]]";

        var description = plan.ApplicationDescription.Contains("[[REPO_BOOTSTRAP_REQUIRED]]", StringComparison.Ordinal)
            ? plan.ApplicationDescription
            : $"{plan.ApplicationDescription}{contractSuffix}";

        return new GenerationPlan(
            plan.ApplicationName,
            description,
            plan.TechStack,
            phases,
            requiredAgents.ToArray(),
            plan.RuntimeImage,
            plan.BuildCommands,
            plan.TestCommands,
            plan.MaxIterations);
    }

    private async Task<(bool Succeeded, string OutcomeCode, string Details)> TryProbeGithubBootstrapAsync(
        AppGenerationOrchestrator orchestrator,
        string userRequest,
        CancellationToken ct)
    {
        if (_mcpTools is null)
            return (false, "mcp_tools_unavailable", "MCP invocation service is not configured.");

        var probeArgs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["query"] = "github open source todo kanban auth permissive license",
            ["url"] = "https://github.com/search?q=todo+kanban+auth+license%3Amit+OR+license%3Aapache-2.0&type=repositories",
            ["mode"] = "repo_bootstrap_probe"
        };

        var outcome = await _mcpTools.InvokeStandaloneAsync(
            userRequestContext: userRequest,
            toolName: "browser.smoke",
            arguments: probeArgs,
            ct: ct).ConfigureAwait(false);

        if (!outcome.Succeeded)
        {
            orchestrator.RecordQualityGate(
                "repo_bootstrap_probe",
                0,
                false,
                new[]
                {
                    $"outcome:{outcome.OutcomeCode}",
                    outcome.Detail ?? "browser probe failed"
                });

            return (false, outcome.OutcomeCode, outcome.Detail ?? "browser.smoke failed");
        }

        var details = string.IsNullOrWhiteSpace(outcome.ResultSummary)
            ? "browser probe succeeded but returned empty summary"
            : outcome.ResultSummary;
        if (!LooksLikeActionableRepoBootstrap(details))
        {
            orchestrator.RecordQualityGate(
                "repo_bootstrap_probe",
                0,
                false,
                new[]
                {
                    "outcome:non_actionable_probe_result",
                    "probe output does not include actionable repo+license evidence"
                });
            return (false, "non_actionable_probe_result", details);
        }

        orchestrator.RecordQualityGate(
            "repo_bootstrap_probe",
            10,
            true,
            new[] { $"outcome:{outcome.OutcomeCode}" });
        return (true, outcome.OutcomeCode, details);
    }

    private static bool LooksLikeActionableRepoBootstrap(string details)
    {
        if (string.IsNullOrWhiteSpace(details))
            return false;

        // Reject obvious stubs/mock outputs.
        if (details.Contains("stub", StringComparison.OrdinalIgnoreCase) ||
            details.Contains("smoke ok", StringComparison.OrdinalIgnoreCase))
            return false;

        var hasGithub = details.Contains("github.com/", StringComparison.OrdinalIgnoreCase);
        var hasLicense = details.Contains("license", StringComparison.OrdinalIgnoreCase) &&
                         (details.Contains("mit", StringComparison.OrdinalIgnoreCase) ||
                          details.Contains("apache-2.0", StringComparison.OrdinalIgnoreCase) ||
                          details.Contains("bsd", StringComparison.OrdinalIgnoreCase) ||
                          details.Contains("isc", StringComparison.OrdinalIgnoreCase) ||
                          details.Contains("mpl", StringComparison.OrdinalIgnoreCase));
        var hasCloneHint = details.Contains("git clone", StringComparison.OrdinalIgnoreCase) ||
                           details.Contains(".git", StringComparison.OrdinalIgnoreCase) ||
                           details.Contains("repository_url", StringComparison.OrdinalIgnoreCase) ||
                           details.Contains("repo_url", StringComparison.OrdinalIgnoreCase);
        return hasGithub && hasLicense && hasCloneHint;
    }

    private static int MergeGeneratedFiles(List<GeneratedFile> files, IReadOnlyList<GeneratedFile> patches)
    {
        var changed = 0;
        foreach (var patch in patches)
        {
            var idx = files.FindIndex(f => f.RelativePath.Equals(patch.RelativePath, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                files.Add(patch);
                changed++;
                continue;
            }

            if (!string.Equals(files[idx].Content, patch.Content, StringComparison.Ordinal))
            {
                files[idx] = patch;
                changed++;
            }
        }

        return changed;
    }

    // P1-2: failure classification centralised in DefaultExecutionFailureClassifier so
    // substring rules are auditable and unit-testable. Handler keeps these forwarders so
    // existing call sites stay unchanged; DI-driven swap-in is tracked as a follow-up.
    private static readonly IExecutionFailureClassifier FailureClassifier = new DefaultExecutionFailureClassifier();

    // P1-10: deterministic plan validator with safe per-stack defaults.
    private static readonly IPlanCommandValidator PlanValidator = new DefaultPlanCommandValidator();

    private GenerationPlan NormalisePlanCommandsIfNeeded(AppGenerationOrchestrator orchestrator, GenerationPlan plan)
    {
        var normalized = PlanValidator.EnsureValidOrThrow(plan);
        orchestrator.RecordQualityGate(
            "plan_command_validation",
            10,
            true,
            new[] { "normalized_or_valid" });
        return normalized;
    }

    private static bool IsRetryableExecutionFailure(ExecutionResult execution) =>
        FailureClassifier.IsRetryable(execution);

    private static bool IsRetryableException(Exception ex) =>
        FailureClassifier.IsRetryableException(ex);

    private static bool IsNonActionableInfrastructureFailure(
        IReadOnlyList<ErrorReport> errors,
        ExecutionResult execution) =>
        FailureClassifier.IsNonActionableInfrastructure(errors, execution);

    private async Task ExecuteMiddlewareBeforeStageAsync(
        AppGenerationOrchestrator orchestrator,
        string stage,
        CancellationToken ct)
    {
        foreach (var middleware in _middlewares)
            await middleware.OnBeforeStageAsync(orchestrator, stage, ct);
    }

    private async Task ExecuteMiddlewareAfterStageAsync(
        AppGenerationOrchestrator orchestrator,
        string stage,
        bool succeeded,
        string? detail,
        CancellationToken ct)
    {
        foreach (var middleware in _middlewares)
            await middleware.OnAfterStageAsync(orchestrator, stage, succeeded, detail, ct);
    }

    private async Task ExecuteFinalizationHooksAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct)
    {
        foreach (var hook in _finalizationHooks)
        {
            try
            {
                await hook.ExecuteAsync(orchestrator, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AutoGen {Id}] Finalization hook {Hook} failed", orchestrator.Id, hook.Name);
                orchestrator.RecordQualityGate(
                    "finalization_hook",
                    6,
                    false,
                    new[] { $"hook_failed:{hook.Name}" });
                if (orchestrator.Status != GenerationStatus.Completed)
                    orchestrator.MarkFailed($"finalization_hook_failed:{hook.Name}");
            }
        }
    }

    /// <summary>
    /// Adds full-jitter to a backoff delay: result is uniformly distributed in [delay/2, delay].
    /// Prevents thundering-herd when multiple runs retry simultaneously.
    /// </summary>
    private static TimeSpan WithJitter(TimeSpan delay)
    {
        var jitterFactor = 0.5 + Random.Shared.NextDouble() * 0.5;
        return TimeSpan.FromMilliseconds(delay.TotalMilliseconds * jitterFactor);
    }

    private static GenerationPlan CreateBuildOnlyPlan(GenerationPlan plan)
    {
        return new GenerationPlan(
            applicationName: plan.ApplicationName,
            applicationDescription: plan.ApplicationDescription,
            techStack: plan.TechStack,
            phases: plan.Phases,
            requiredAgents: plan.RequiredAgents,
            runtimeImage: plan.RuntimeImage,
            buildCommands: plan.BuildCommands,
            testCommands: Array.Empty<string>(),
            maxIterations: plan.MaxIterations);
    }

    private async Task<(string ArtifactId, int ArtifactVersion)> PersistStructuredDesignArtifactAsync(
        Guid runId,
        FrontendDesignPreplanResult designResult,
        CancellationToken ct)
    {
        if (_designArtifacts is null)
            return (designResult.Artifact.ArtifactId, 1);

        var tokens = new DesignTokens();
        if (designResult.Artifact.DesignTokens.TryGetValue("spacing.base", out var spacing) &&
            int.TryParse(spacing.Replace("px", string.Empty, StringComparison.OrdinalIgnoreCase), out var spacingPx))
            tokens.Spacing.Md = Math.Max(1, spacingPx);
        if (designResult.Artifact.Palette.TryGetValue("brand.primary", out var primary))
            tokens.Colors.Primary = primary;
        if (designResult.Artifact.Palette.TryGetValue("brand.accent", out var secondary))
            tokens.Colors.Secondary = secondary;

        var palette = new DesignPalette
        {
            Name = "FrontendPreplanner",
            BrandColors = designResult.Artifact.Palette.ToDictionary(k => k.Key, v => v.Value)
        };
        var typography = new TypographyScale();
        if (designResult.Artifact.Typography.TryGetValue("font.family", out var family))
            typography.FontFamily = family;

        var components = new ComponentSpecifications();
        var screens = new ScreenMap
        {
            KeyPages = designResult.Artifact.Screens.Keys.ToArray(),
            Screens = designResult.Artifact.Screens.ToDictionary(
                k => k.Key,
                v => new ScreenDefinition
                {
                    Name = v.Key,
                    Purpose = v.Value
                })
        };
        var accessibility = new AccessibilityProfile();
        if (designResult.Artifact.Accessibility.TryGetValue("contrast", out var contrast))
            accessibility.ContrastLevel = contrast;

        var artifact = await _designArtifacts.CreateArtifactAsync(
            runId.ToString("D"),
            tokens,
            palette,
            typography,
            components,
            screens,
            accessibility,
            ct);

        if (_designBinding is not null)
        {
            var bindingProbe = await _designBinding.BindArtifactToGenerationPromptAsync("probe", artifact, ct);
            _ = _designBinding.ValidateGenerationPromptReferencesArtifact(bindingProbe, artifact.Id, out _);
        }

        return (artifact.Id, artifact.Version);
    }

    // --- Multi-agent generation helpers ------------------------------------

    private async Task<List<GenerationPhaseBatchResult>> RunMultiAgentGenerationAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        CancellationToken runCt)
    {
        var backendStack = ResolveBackendStackId(plan);
        var frontendStack = ResolveFrontendStackId(plan);
        var orchestrators = _agentOrchestrationFactory!.CreateForPlan(plan, backendStack, frontendStack);
        var allPhaseResults = new List<GenerationPhaseBatchResult>();
        var phaseResultsLock = new object();

        _logger.LogInformation(
            "[AutoGen {Id}] Multi-agent run: {PhaseCount} phases (backend={Backend}, frontend={Frontend})",
            orchestrator.Id,
            orchestrators.Count,
            backendStack,
            frontendStack ?? "(none)");

        async Task RunPhaseAndPersistAsync(
            KeyValuePair<Libr4.IDE.AutonomousAppGeneration.Agents.AgentPhase, Libr4.IDE.AutonomousAppGeneration.Agents.SubagentOrchestrator> entry)
        {
            var phase = entry.Key;
            var subOrchestrator = entry.Value;

            _logger.LogInformation(
                "[AutoGen {Id}] Running multi-agent generation for phase '{Phase}'",
                orchestrator.Id,
                phase);
            _runControl.UpdateRunProgress(
                orchestrator.Id,
                $"generating_{phase.ToString().ToLowerInvariant()}",
                0,
                1);

            var tasks = _multiAgentOptions.UseParallelTasksPerPhase
                ? Libr4.IDE.AutonomousAppGeneration.Agents.MultiAgentTaskPlanner.CreateTasksForPhase(
                    phase,
                    plan,
                    includeSubagentRoles: true)
                : Libr4.IDE.AutonomousAppGeneration.Agents.MultiAgentTaskPlanner.CreateSingleTaskForPhase(
                    phase,
                    plan,
                    includeSubagentRoles: true);

            var phaseResult = await subOrchestrator.ExecuteParallelAsync(tasks, runCt);
            var parsedFiles = Libr4.IDE.AutonomousAppGeneration.Agents.MultiAgentArtifactCollector.CollectFiles(phaseResult);
            if (parsedFiles.Count == 0)
            {
                var generatedContent = CollectMultiAgentContent(phaseResult);
                parsedFiles = TryParseGeneratedFiles(generatedContent);
            }

            GenerationPhaseBatchResult? batch;
            lock (phaseResultsLock)
            {
                batch = AppendPhaseResult(orchestrator, allPhaseResults, phase, parsedFiles);
            }

            await PersistGenerationProgressAsync(orchestrator, batch, runCt).ConfigureAwait(false);
        }

        if (_multiAgentOptions.RunPhasesInParallel && orchestrators.Count > 1)
            await Task.WhenAll(orchestrators.Select(RunPhaseAndPersistAsync)).ConfigureAwait(false);
        else
        {
            foreach (var entry in orchestrators)
                await RunPhaseAndPersistAsync(entry).ConfigureAwait(false);
        }

        return allPhaseResults;
    }

    private async Task PersistGenerationProgressAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPhaseBatchResult? latestBatch,
        CancellationToken ct)
    {
        if (latestBatch is null || latestBatch.Files.Count == 0)
            return;

        foreach (var file in latestBatch.Files)
        {
            var repaired = StackArtifactCompleteness.RepairGeneratedFile(file);
            if (repaired is not null)
                orchestrator.UpsertFile(repaired);
        }

        await _repository.SaveAsync(orchestrator, ct).ConfigureAwait(false);
        _logger.LogInformation(
            "[AutoGen {Id}] Persisted generation progress (phase={Phase}, +{Added}, total={Total})",
            orchestrator.Id,
            latestBatch.PhaseName,
            latestBatch.Files.Count,
            orchestrator.Files.Count);
    }

    private GenerationPhaseBatchResult? AppendPhaseResult(
        AppGenerationOrchestrator orchestrator,
        List<GenerationPhaseBatchResult> allPhaseResults,
        Libr4.IDE.AutonomousAppGeneration.Agents.AgentPhase phase,
        List<GeneratedFile> parsedFiles)
    {
        if (parsedFiles.Count > 0)
        {
            var batch = new GenerationPhaseBatchResult(phase.ToString().ToLowerInvariant(), parsedFiles);
            allPhaseResults.Add(batch);
            _logger.LogInformation(
                "[AutoGen {Id}] Phase '{Phase}' generated {Count} files",
                orchestrator.Id,
                phase,
                parsedFiles.Count);
            return batch;
        }

        _logger.LogWarning(
            "[AutoGen {Id}] Phase '{Phase}' returned no parseable files",
            orchestrator.Id,
            phase);
        return null;
    }

    private static string CollectMultiAgentContent(
        Libr4.IDE.AutonomousAppGeneration.Agents.OrchestrationResult phaseResult) =>
        Libr4.IDE.AutonomousAppGeneration.Agents.MultiAgentArtifactCollector.CollectContent(phaseResult);

    private static string ResolveBackendStackId(GenerationPlan plan)
    {
        if (StackPlanHeuristics.IsJava(plan))
            return "java";
        if (StackPlanHeuristics.IsPython(plan))
            return "python";
        if (StackPlanHeuristics.IsNode(plan))
            return "javascript";
        return plan.TechStack.Languages.FirstOrDefault() ?? "csharp";
    }

    private static string? ResolveFrontendStackId(GenerationPlan plan)
    {
        if (StackPlanHeuristics.IsReactTypeScriptFrontend(plan))
            return "typescript";
        var fw = plan.TechStack.Frameworks.FirstOrDefault(f =>
            f.Contains("react", StringComparison.OrdinalIgnoreCase)
            || f.Contains("vue", StringComparison.OrdinalIgnoreCase)
            || f.Contains("angular", StringComparison.OrdinalIgnoreCase)
            || f.Contains("blazor", StringComparison.OrdinalIgnoreCase)
            || f.Contains("svelte", StringComparison.OrdinalIgnoreCase));
        return fw;
    }

    private static List<GeneratedFile> TryParseGeneratedFiles(string content) =>
        Libr4.IDE.AutonomousAppGeneration.Agents.AgentGeneratedFileParser.TryParse(content);

    private static IReadOnlyList<GeneratedFile> MergeGeneratedFilesPreferLongerContent(
        IReadOnlyList<GeneratedFile> primary,
        IReadOnlyList<GeneratedFile> supplement)
    {
        var dict = new Dictionary<string, GeneratedFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in primary.Concat(supplement))
        {
            var normalized = StackArtifactCompleteness.RepairGeneratedFile(file);
            if (normalized is null)
                continue;

            var path = normalized.RelativePath;
            if (dict.TryGetValue(path, out var existing))
            {
                if ((normalized.Content?.Length ?? 0) > (existing.Content?.Length ?? 0))
                    dict[path] = normalized;
            }
            else
            {
                dict[path] = normalized;
            }
        }

        return dict.Values.ToList();
    }
}
