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
        Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationFactory? agentOrchestrationFactory = null)
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
            finalizationHooks: null)
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

        var fingerprint = BuildFingerprint(
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

            // P1-3: delegate planning prefix to pipeline runner when opted-in.
            GenerationPlan plan;
            if (_loopGuardOptions.UsePipelineRunnerForPlanningPrefix && _pipelineRunner is not null)
            {
                var pipelineCtx = new GenerationContext
                {
                    Orchestrator = orchestrator,
                    UserRequest = normalizedRequest,
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
                    : await _planner.PlanAsync(normalizedRequest, runCt);

            if (resumeSource?.Plan is not null)
            {
                orchestrator.RecordQualityGate("resume_seed_plan", 10, true, new[] { $"source_run:{resumeSource.Id}" });
            }
            if (_teamTemplateResolver is not null)
            {
                var templateResolution = _teamTemplateResolver.Resolve(normalizedRequest);
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
                var routing = _subagentRoutingService.Resolve(normalizedRequest);
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
            // P1-10 of audit: detect malformed build/test commands BEFORE generation begins.
            // Substituting safe defaults prevents the legacy 8-iteration burn pattern observed
            // in ENHANCED_GENERATION_TEST_RESULTS where a quoting bug in the plan blocked all fixes.
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
                    // Multi-agent generation: each phase gets its own specialist agent
                    var backendStack = plan.TechStack.Languages.FirstOrDefault() ?? "csharp";
                    var frontendStack = plan.TechStack.Frameworks.FirstOrDefault(f =>
                        f.Contains("react", StringComparison.OrdinalIgnoreCase) ||
                        f.Contains("vue", StringComparison.OrdinalIgnoreCase) ||
                        f.Contains("angular", StringComparison.OrdinalIgnoreCase) ||
                        f.Contains("blazor", StringComparison.OrdinalIgnoreCase) ||
                        f.Contains("svelte", StringComparison.OrdinalIgnoreCase));

                    var orchestrators = _agentOrchestrationFactory.CreateFullStackOrchestrators(backendStack, frontendStack);
                    var allPhaseResults = new List<GenerationPhaseBatchResult>();

                    foreach (var (phase, subOrchestrator) in orchestrators)
                    {
                        _logger.LogInformation(
                            "[AutoGen {Id}] Running multi-agent generation for phase '{Phase}'",
                            orchestrator.Id, phase);

                        var tasks = new List<Libr4.IDE.AutonomousAppGeneration.Agents.AgentTask>
                        {
                            new()
                            {
                                Description = $"Generate {phase} artifacts for {plan.ApplicationName}",
                                Context = new Libr4.IDE.AutonomousAppGeneration.Agents.AgentContext
                                {
                                    ApplicationName = plan.ApplicationName,
                                    Description = plan.ApplicationDescription,
                                    TechStack = string.Join(", ", plan.TechStack.Languages.Concat(plan.TechStack.Frameworks))
                                }
                            }
                        };

                        var phaseResult = await subOrchestrator.ExecuteParallelAsync(tasks, runCt);
                        var generatedContent = string.Join("\n", phaseResult.Results
                            .Where(r => r.IsSuccess)
                            .Select(r => r.Result?.Content ?? string.Empty));

                        var parsedFiles = TryParseGeneratedFiles(generatedContent);
                        if (parsedFiles.Count > 0)
                        {
                            allPhaseResults.Add(new GenerationPhaseBatchResult(phase.ToString().ToLowerInvariant(), parsedFiles));
                            _logger.LogInformation(
                                "[AutoGen {Id}] Phase '{Phase}' generated {Count} files",
                                orchestrator.Id, phase, parsedFiles.Count);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[AutoGen {Id}] Phase '{Phase}' returned no parseable files. Content length: {Len}",
                                orchestrator.Id, phase, generatedContent.Length);
                        }
                    }

                    phaseBatches = TechStackArtifactFilter.PrunePhaseBatches(allPhaseResults, plan);
                    files = phaseBatches.SelectMany(p => p.Files).ToList();
                }
                else
                {
                    // Fallback to monolithic generation when multi-agent infra is not available
                    phaseBatches = TechStackArtifactFilter.PrunePhaseBatches(
                        await _codeGen.GenerateInitialByPhasesAsync(plan, runCt),
                        plan);
                    files = phaseBatches.SelectMany(p => p.Files).ToList();
                }
            }
            if (files.Count == 0)
            {
                var merged = GenerationStackSafetyNet.MergeWithStackSafetyNet(plan, files);
                phaseBatches = new List<GenerationPhaseBatchResult>
                {
                    new GenerationPhaseBatchResult("post_prune_safety", merged)
                };
                files = merged.ToList();
                _logger.LogWarning(
                    "[AutoGen {Id}] All generated artifacts were pruned or absent; applied stack safety-net ({Count} files)",
                    orchestrator.Id, files.Count);
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

            foreach (var file in files) orchestrator.UpsertFile(file);
            await _repository.SaveAsync(orchestrator, ct);

            await _agentIntegration.IngestGenerationArtifactsAsync(orchestrator, plan, files, runCt)
                .ConfigureAwait(false);

            var securityReview = _agentIntegration.ReviewGeneratedCode("post_generation", files, plan);
            orchestrator.RecordSecurityReview(securityReview);
            if (!securityReview.Passed)
            {
                await _agentIntegration.OnGateFailureAsync(
                    orchestrator, "security_review", securityReview.Reasons, runCt).ConfigureAwait(false);
                orchestrator.MarkFailed(
                    $"security_review_failed: score={securityReview.Score}; reasons={string.Join(",", securityReview.Reasons)}");
                await ExecuteMiddlewareAfterStageAsync(orchestrator, "generation", false, orchestrator.FailureReason, runCt);
                await _repository.SaveAsync(orchestrator, ct);
                return new AppGenerationResponse(
                    Id: orchestrator.Id,
                    Status: orchestrator.Status.ToString(),
                    ApplicationName: plan.ApplicationName,
                    Iterations: orchestrator.Iterations.Count,
                    MaxIterations: plan.MaxIterations,
                    Succeeded: false,
                    FailureReason: orchestrator.FailureReason ?? "security_review_failed");
            }

            var generationGate = _qualityGates.EvaluateGeneratedFiles(files, plan);
            orchestrator.RecordQualityGate(generationGate.Stage, generationGate.Score, generationGate.Passed, generationGate.Reasons);
            if (!generationGate.Passed)
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
                var baselineMetrics = orchestrator.QualityGates
                    .Select(q => new QualityGateResult(q.Stage, q.Score, q.Passed, q.Reasons))
                    .ToList();
                var reviewDecision = _reviewGate2.EvaluateComprehensive("post_generation", files, plan, baselineMetrics);
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

                            var retryDecision = _reviewGate2.EvaluateComprehensive("post_generation_retry", files, plan, baselineMetrics);
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

                if (consecutiveSameError >= Math.Max(2, _loopGuardOptions.SameErrorThreshold))
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

                if (consecutiveNoProgress >= Math.Max(2, _loopGuardOptions.NoProgressThreshold))
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
                orchestrator.MarkFailed($"iteration_budget_exceeded: exceeded iteration budget of {plan.MaxIterations}");
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
                    orchestrator.RecordQualityGate(
                        "final_report",
                        isValidShape ? 10 : 6,
                        isValidShape,
                        new[] { $"payload_bytes={Encoding.UTF8.GetByteCount(reportJson)}" });
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

    // P1-2: failure classification centralised in DefaultExecutionFailureClassifier so
    // substring rules are auditable and unit-testable. Handler keeps these forwarders so
    // existing call sites stay unchanged; DI-driven swap-in is tracked as a follow-up.
    private static readonly IExecutionFailureClassifier FailureClassifier = new DefaultExecutionFailureClassifier();

    // P1-10: deterministic plan validator with safe per-stack defaults.
    private static readonly IPlanCommandValidator PlanValidator = new DefaultPlanCommandValidator();

    private GenerationPlan NormalisePlanCommandsIfNeeded(AppGenerationOrchestrator orchestrator, GenerationPlan plan)
    {
        var validation = PlanValidator.Validate(plan);
        if (validation.IsValid)
            return plan;

        var (safeBuild, safeTest) = PlanValidator.GetSafeDefaults(plan);
        _logger.LogWarning(
            "[AutoGen {Id}] Plan command validation failed ({Issues}). Substituting safe stack defaults.",
            orchestrator.Id, string.Join(",", validation.Issues));

        orchestrator.RecordQualityGate(
            "plan_command_validation",
            8,
            true,
            new[] { $"issues:{string.Join(",", validation.Issues)}", "fallback:safe_defaults_applied" });

        return new GenerationPlan(
            plan.ApplicationName,
            plan.ApplicationDescription,
            plan.TechStack,
            plan.Phases,
            plan.RequiredAgents,
            plan.RuntimeImage,
            safeBuild,
            safeTest,
            plan.MaxIterations);
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

    private static string BuildFingerprint(
        string userRequest,
        int maxIterations,
        string? triggerSource = null,
        string? triggerActor = null,
        string? tenantId = null)
    {
        var normalized = string.Concat(
            (userRequest ?? string.Empty).Trim().ToLowerInvariant(),
            "|",
            maxIterations.ToString(),
            "|",
            (triggerSource ?? string.Empty).ToLowerInvariant(),
            "|",
            (triggerActor ?? string.Empty).ToLowerInvariant(),
            "|",
            // P2-3: tenant-scoped fingerprint — same prompt from different tenants = distinct runs.
            (tenantId ?? string.Empty).ToLowerInvariant());
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
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

    /// <summary>
    /// Attempts to parse a JSON response containing generated files into a list of GeneratedFile objects.
    /// Handles both explicit JSON objects and inline file markers like "// File: path".
    /// </summary>
    private static List<GeneratedFile> TryParseGeneratedFiles(string content)
    {
        var files = new List<GeneratedFile>();
        if (string.IsNullOrWhiteSpace(content))
            return files;

        // Try explicit JSON first
        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                content,
                @"\{\s*""files""\s*:\s*\[(.*?)\]\s*\}",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (jsonMatch.Success)
            {
                var json = jsonMatch.Value;
                using var doc = JsonDocument.Parse(json);
                var filesArray = doc.RootElement.GetProperty("files");
                foreach (var element in filesArray.EnumerateArray())
                {
                    var path = element.GetProperty("relativePath").GetString();
                    var fileContent = element.GetProperty("content").GetString();
                    if (!string.IsNullOrWhiteSpace(path) && fileContent is not null)
                    {
                        var lang = Path.GetExtension(path).TrimStart('.').ToLowerInvariant() switch
                        {
                            "cs" => "csharp",
                            "ts" or "tsx" => "typescript",
                            "js" or "jsx" => "javascript",
                            "py" => "python",
                            "go" => "go",
                            "rs" => "rust",
                            "java" => "java",
                            "php" => "php",
                            "rb" => "ruby",
                            "kt" => "kotlin",
                            "scala" => "scala",
                            "swift" => "swift",
                            "dart" => "dart",
                            "html" => "html",
                            "css" => "css",
                            "scss" or "sass" => "scss",
                            "sql" => "sql",
                            "yaml" or "yml" => "yaml",
                            "json" => "json",
                            "xml" => "xml",
                            "dockerfile" => "dockerfile",
                            "sh" or "bash" => "shell",
                            "ps1" => "powershell",
                            "md" => "markdown",
                            _ => "plaintext"
                        };
                        files.Add(new GeneratedFile(path, lang, fileContent));
                    }
                }
                return files;
            }
        }
        catch { /* fallback to regex parsing */ }

        // Fallback: parse "// File: path" markers
        var fileMarkerRegex = new System.Text.RegularExpressions.Regex(
            @"(?://|#)\s*File:\s*(.+?)\r?\n(.*?)(?=(?://|#)\s*File:|$)",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        foreach (System.Text.RegularExpressions.Match match in fileMarkerRegex.Matches(content))
        {
            var path = match.Groups[1].Value.Trim();
            var fileContent = match.Groups[2].Value.Trim();
            if (!string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(fileContent))
            {
                files.Add(new GeneratedFile(path, "plaintext", fileContent));
            }
        }

        return files;
    }
}
