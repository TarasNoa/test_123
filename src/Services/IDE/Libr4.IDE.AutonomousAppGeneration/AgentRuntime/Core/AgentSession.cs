using System.Text.Json;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Infrastructure.AI;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Persistence;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting.Templates;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;
using FSharpTurn = Libr4.IDE.AutonomousAppGeneration.Algorithms.AgentRuntime.AgentSessionTurnMachine;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Fim;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public sealed class AgentSession : IAgentSession
{
    private readonly IAIService _ai;
    private readonly IProviderCapabilityMatrix _providerMatrix;
    private readonly IAgentModelRouter _modelRouter;
    private readonly IBudgetService? _budget;
    private readonly IRunProviderCostTracker? _costTracker;
    private readonly IAgentToolRegistry _registry;
    private readonly ToolOrchestrator _orchestrator;
    private readonly IContextCompactor _compactor;
    private readonly IAgentSessionStore? _sessionStore;
    private readonly IAgentSessionResumeService? _resumeService;
    private readonly IRolloutRecorder? _rollout;
    private readonly INdjsonEventWriter? _ndjson;
    private readonly IAgentLifecycleHookRunner? _lifecycle;
    private readonly IBuiltinPromptVarResolver _promptVars;
    private readonly IPromptTemplateRegistry? _promptTemplates;
    private readonly PromptVariantSelector? _promptVariants;
    private readonly IFimPromptBuilder? _fimBuilder;
    private readonly IDelegationManager? _delegation;
    private readonly IBackgroundFleetScheduler? _fleetScheduler;
    private readonly IHermesMemoryManager? _hermesMemory;
    private readonly AgentRuntimeOptions _options;
    private readonly ILogger<AgentSession> _logger;

    public AgentSession(
        IAIService ai,
        IProviderCapabilityMatrix providerMatrix,
        IAgentModelRouter modelRouter,
        IAgentToolRegistry registry,
        ToolOrchestrator orchestrator,
        IContextCompactor compactor,
        IOptions<AgentRuntimeOptions> options,
        ILogger<AgentSession> logger,
        IBuiltinPromptVarResolver promptVars,
        IPromptTemplateRegistry? promptTemplates = null,
        PromptVariantSelector? promptVariants = null,
        IAgentSessionStore? sessionStore = null,
        IAgentSessionResumeService? resumeService = null,
        IRolloutRecorder? rollout = null,
        INdjsonEventWriter? ndjson = null,
        IAgentLifecycleHookRunner? lifecycle = null,
        IDelegationManager? delegation = null,
        IBackgroundFleetScheduler? fleetScheduler = null,
        IFimPromptBuilder? fimBuilder = null,
        IHermesMemoryManager? hermesMemory = null,
        IBudgetService? budget = null,
        IRunProviderCostTracker? costTracker = null)
    {
        _ai = ai;
        _providerMatrix = providerMatrix;
        _modelRouter = modelRouter;
        _registry = registry;
        _orchestrator = orchestrator;
        _compactor = compactor;
        _sessionStore = sessionStore;
        _resumeService = resumeService ?? sessionStore as IAgentSessionResumeService;
        _rollout = rollout;
        _ndjson = ndjson;
        _lifecycle = lifecycle;
        _promptVars = promptVars;
        _promptTemplates = promptTemplates;
        _promptVariants = promptVariants;
        _fimBuilder = fimBuilder;
        _delegation = delegation;
        _fleetScheduler = fleetScheduler;
        _hermesMemory = hermesMemory;
        _options = options.Value;
        _logger = logger;
        _budget = budget;
        _costTracker = costTracker;
    }

    public Task<AgentSessionResult> RunAsync(
        string objective,
        ShadowWorkspaceContext workspace,
        IList<GeneratedFile> workingFiles,
        GenerationPlan plan,
        IShadowWorkspaceAccessor accessor,
        string? buildLog,
        CancellationToken ct = default) =>
        RunAsync(new AgentSessionRunRequest(
            objective,
            workspace,
            workingFiles,
            plan,
            accessor,
            AgentSessionMode.Repair,
            buildLog), ct);

    public async Task<AgentSessionResult> ResumeAsync(string sessionId, AgentSessionRunRequest request, CancellationToken ct = default)
    {
        if (_resumeService is null)
            return await RunAsync(request, ct).ConfigureAwait(false);

        var bundle = await _resumeService.LoadResumeBundleAsync(sessionId, ct).ConfigureAwait(false);
        if (bundle is null)
            return await RunAsync(request with { ResumeSessionId = null }, ct).ConfigureAwait(false);

        return await RunAsync(request with { ResumeSessionId = sessionId }, ct).ConfigureAwait(false);
    }

    public async Task<string> CheckpointAsync(string sessionId, IReadOnlyList<AgentConversationTurn> turns, CancellationToken ct = default)
    {
        if (_sessionStore is null)
            return Guid.NewGuid().ToString("D");

        var checkpointId = Guid.NewGuid().ToString("D");
        var messagesJson = JsonSerializer.Serialize(turns);
        await _sessionStore.SaveCheckpointAsync(new AgentCheckpointRecord(
            checkpointId,
            sessionId,
            turns.Count,
            messagesJson,
            "{}",
            DateTime.UtcNow), ct).ConfigureAwait(false);
        return checkpointId;
    }

    public async Task<IReadOnlyList<AgentConversationTurn>> RewindAsync(string sessionId, string checkpointId, CancellationToken ct = default)
    {
        if (_sessionStore is null)
            return Array.Empty<AgentConversationTurn>();

        var checkpoint = await _sessionStore.GetCheckpointAsync(checkpointId, ct).ConfigureAwait(false);
        if (checkpoint is null || !string.Equals(checkpoint.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
            return Array.Empty<AgentConversationTurn>();

        return JsonSerializer.Deserialize<List<AgentConversationTurn>>(checkpoint.MessagesJson)
               ?? new List<AgentConversationTurn>();
    }

    public async Task<AgentSessionResult> RunAsync(AgentSessionRunRequest request, CancellationToken ct = default)
    {
        if (_options.EnableSessionPersistence && _sessionStore is not null)
            await _sessionStore.EnsureSchemaAsync(ct).ConfigureAwait(false);

        var trace = new List<string>();
        var isGeneration = request.Mode == AgentSessionMode.Generation;
        var maxTurns = request.MaxTurns ?? (isGeneration
            ? _options.MaxTurnsPerGenerationFile
            : _options.MaxTurnsPerRepair);

        List<AgentConversationTurn> turns;
        var sessionState = new AgentSessionState
        {
            RunId = request.RunId,
            TenantUserId = request.TenantUserId,
            SpaceId = request.SpaceId,
            LastErrors = request.LastErrors,
            PermissionMode = _options.DefaultPermissionMode,
            FimContext = request.Fim
        };

        if (!string.IsNullOrWhiteSpace(request.ResumeSessionId) && _resumeService is not null)
        {
            var bundle = await _resumeService.LoadResumeBundleAsync(request.ResumeSessionId, ct).ConfigureAwait(false);
            if (bundle is not null)
            {
                sessionState.SessionId = bundle.Session.SessionId;
                sessionState.CurrentStepNumber = bundle.NextStepNumber;
                turns = bundle.Turns.ToList();
                trace.Add($"resume:{request.ResumeSessionId}:step={bundle.NextStepNumber}");
            }
            else
            {
                turns = BuildInitialTurns(request, isGeneration);
            }
        }
        else
        {
            turns = BuildInitialTurns(request, isGeneration);
        }

        if (_options.EnableSessionPersistence && _sessionStore is not null && string.IsNullOrWhiteSpace(request.ResumeSessionId))
        {
            await _sessionStore.CreateSessionAsync(new AgentSessionRecord(
                sessionState.SessionId,
                request.RunId,
                null,
                null,
                "running",
                DateTime.UtcNow,
                DateTime.UtcNow,
                0,
                0,
                sessionState.PermissionMode.ToString(),
                0), ct).ConfigureAwait(false);
        }

        var fileState = new FileStateCache();
        if (!string.IsNullOrWhiteSpace(request.ResumeSessionId) && _sessionStore is not null)
        {
            var toolCalls = await _sessionStore.GetToolCallsAsync(sessionState.SessionId, ct).ConfigureAwait(false);
            FileStateCacheRestorer.RestoreFromToolCalls(fileState, toolCalls);
            trace.Add($"resume_file_cache:{toolCalls.Count(c => c.Success && string.Equals(c.ToolName, "read_file", StringComparison.OrdinalIgnoreCase))}");
        }

        var varContext = BuildVarContext(request, isGeneration, sessionState);

        var context = new ToolContext
        {
            Workspace = request.Workspace,
            Accessor = request.Accessor,
            WorkingFiles = request.WorkingFiles,
            FileState = fileState,
            Plan = request.Plan,
            BuildLog = request.BuildLog,
            Mode = request.Mode,
            Session = sessionState,
            ToolInput = JsonDocument.Parse("{}").RootElement,
            AllowedTools = request.AllowedTools
        };

        if (_lifecycle is not null)
        {
            await _lifecycle.RunAsync(AgentHookKind.SessionStart, new HookContext
            {
                RunId = request.RunId,
                SessionId = sessionState.SessionId,
                WorkspaceRoot = request.Workspace.HostPath
            }, ct).ConfigureAwait(false);
        }

        var allPatches = new Dictionary<string, GeneratedFile>(StringComparer.OrdinalIgnoreCase);
        var promptStage = request.PromptStage ?? (isGeneration
            ? BuiltinPromptStage.Generating
            : BuiltinPromptStage.Repairing);
        var stage = isGeneration ? "generation" : "fixing";
        var consecutiveReadOnlyTools = 0;
        var consecutiveInvalidTurns = 0;
        var startTurn = sessionState.CurrentStepNumber > 0 ? sessionState.CurrentStepNumber : 1;
        var requestFingerprint = request.SpaceId is Guid spaceId
            ? HermesMemoryScopeResolver.BuildSpaceFingerprint(spaceId)
            : _hermesMemory?.ResolveFingerprint(request.Plan, request.RequestFingerprint)
              ?? request.RequestFingerprint
              ?? string.Empty;

        for (var turn = startTurn; turn <= maxTurns; turn++)
        {
            ct.ThrowIfCancellationRequested();
            sessionState.CurrentStepNumber = turn;
            await EmitStepStartAsync(request.RunId, sessionState.SessionId, turn, ct).ConfigureAwait(false);
            await InjectDelegationNotificationsAsync(request.RunId, turns, ct).ConfigureAwait(false);

            if (_hermesMemory is not null && !string.IsNullOrWhiteSpace(requestFingerprint))
            {
                var memoryNudge = await _hermesMemory.PrefetchBeforeTurnAsync(
                    new HermesTurnContext(
                        request.RunId,
                        requestFingerprint,
                        stage,
                        request.LastErrors),
                    ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(memoryNudge))
                    turns.Add(new AgentConversationTurn("system", memoryNudge, DateTime.UtcNow));
            }

            var turnVarContext = BuildVarContext(request, isGeneration, sessionState);
            var compacted = await _compactor.CompactAsync(
                turns,
                _options.ConversationCharBudget,
                new CompactionRequest(
                    request.RunId,
                    sessionState.SessionId,
                    request.ManifestFiles,
                    requestFingerprint,
                    stage),
                ct).ConfigureAwait(false);
            var prompt = AgentPromptBuilder.BuildTurnPrompt(compacted);
            var promptRole = AgentPromptBuilder.MapStageToRole(promptStage, isGeneration);
            var promptVariant = _promptVariants?.SelectVariant(promptRole, request.RunId);
            var turnSystemPrompt = AgentPromptBuilder.BuildSystemPrompt(
                isGeneration,
                promptStage,
                _promptVars,
                turnVarContext,
                _promptTemplates,
                promptVariant);

            string? raw = null;
            Exception? lastLlmError = null;
            for (var attempt = 1; attempt <= _options.LlmRetryAttempts; attempt++)
            {
                try
                {
                    raw = await GenerateAgentCompletionAsync(prompt, turnSystemPrompt, stage, request, ct).ConfigureAwait(false);
                    lastLlmError = null;
                    break;
                }
                catch (BudgetExceededException)
                {
                    throw;
                }
                catch (Exception ex) when (attempt < _options.LlmRetryAttempts)
                {
                    lastLlmError = ex;
                    _logger.LogWarning(ex, "Agent session LLM call failed on turn {Turn} attempt {Attempt}/{Max}", turn, attempt, _options.LlmRetryAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(2 * attempt, 8)), ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    lastLlmError = ex;
                    break;
                }
            }

            if (raw is null)
            {
                trace.Add($"turn_{turn}:llm_error:{lastLlmError?.Message}");
                await EmitErrorAsync(request.RunId, sessionState.SessionId, lastLlmError?.Message ?? "llm_error", ct).ConfigureAwait(false);
                break;
            }

            var parsed = AgentResponseParser.Parse(raw, stripReasoning: !_options.IncludeReasoningInContext);
            if (!string.IsNullOrWhiteSpace(parsed.Reasoning))
            {
                sessionState.ReasoningLog.Add(parsed.Reasoning!);
                if (request.RunId is Guid runId && _ndjson is not null && _options.EnableNdjsonEvents)
                    await _ndjson.WriteAsync(runId, new { type = "reasoning", sessionId = sessionState.SessionId, stepNumber = turn, text = parsed.Reasoning }, ct).ConfigureAwait(false);
            }

            turns.Add(new AgentConversationTurn("assistant", parsed.Raw, DateTime.UtcNow));
            await PersistTurnAsync(sessionState.SessionId, turn, turns[^1], null, ct).ConfigureAwait(false);

            if (parsed.Action == AgentTurnAction.Done)
            {
                if (FSharpAlgorithmsBridge.DecideAfterParse(
                    isGeneration,
                    parsed.Action,
                    parsed.ToolCall is not null,
                    allPatches,
                    request.TargetRelativePaths) == FSharpTurn.AfterParseDecision.RejectDoneMissingTargets)
                {
                    trace.Add($"turn_{turn}:done_rejected:missing_targets");
                    turns.Add(new AgentConversationTurn(
                        "system",
                        "done rejected: TARGET FILE(S) not written yet. Call write_file with complete content for each target, then done.",
                        DateTime.UtcNow));
                    continue;
                }

                trace.Add($"turn_{turn}:done:{parsed.Summary}");
                await EmitStepFinishAsync(request.RunId, sessionState.SessionId, turn, "done", ct).ConfigureAwait(false);
                var doneResult = new AgentSessionResult(
                    true,
                    parsed.Summary ?? "done",
                    FilterPatches(allPatches, request.TargetRelativePaths),
                    turn,
                    trace);
                await MarkSessionCompletedAsync(sessionState.SessionId, ct).ConfigureAwait(false);
                await EmitSessionEndAsync(request, sessionState, ct).ConfigureAwait(false);
                _logger.LogInformation("Agent session completed: {Summary}", AgentRuntimeTelemetry.FormatSummary(request.Mode, doneResult));
                return doneResult;
            }

            AgentToolCall? toolCall = parsed.ToolCall;
            if (parsed.Action != AgentTurnAction.Tool || toolCall is null)
            {
                consecutiveInvalidTurns++;
                trace.Add($"turn_{turn}:invalid:{parsed.Summary}");

                if (isGeneration && _options.EnableToolCallRecovery)
                {
                    var recovery = ToolCallRecovery.Recover(
                        parsed.Raw,
                        consecutiveInvalidTurns,
                        request.TargetRelativePaths,
                        request.WorkingFiles,
                        request.Plan,
                        _options.EnableRawContentCoercion,
                        _options.EnableBoilerplateFallback);

                    if (recovery.HasToolCall)
                    {
                        toolCall = recovery.ToolCall;
                        consecutiveInvalidTurns = 0;
                        trace.Add($"turn_{turn}:recovery:{recovery.Stage}");
                    }
                    else if (recovery.RequiresNudge)
                    {
                        turns.Add(new AgentConversationTurn("system", recovery.SystemNudge!, DateTime.UtcNow));
                        if (recovery.Stage is not null)
                            trace.Add($"turn_{turn}:recovery_nudge:{recovery.Stage}");
                        continue;
                    }
                }

                if (toolCall is null)
                {
                    turns.Add(new AgentConversationTurn(
                        "system",
                        "Invalid response. Reply ONLY with JSON: {\"action\":\"tool\",...} or {\"action\":\"done\",...}",
                        DateTime.UtcNow));
                    continue;
                }
            }
            else
            {
                consecutiveInvalidTurns = 0;
            }

            var toolStartedAt = DateTimeOffset.UtcNow;
            var result = await _orchestrator.ExecuteAsync(toolCall, context, ct).ConfigureAwait(false);
            foreach (var patch in result.FilePatches)
                allPatches[patch.RelativePath] = patch;

            if (request.RunId is Guid runIdForTool && _ndjson is not null && _options.EnableNdjsonEvents)
            {
                await _ndjson.WriteAsync(runIdForTool, new
                {
                    type = "tool_use",
                    sessionId = sessionState.SessionId,
                    stepNumber = turn,
                    toolName = result.ToolName,
                    success = result.Success,
                    timing = new
                    {
                        startedAt = toolStartedAt.ToUnixTimeMilliseconds(),
                        finishedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        durationMs = sessionState.LastToolDurationMs
                    },
                    media = result.FilePatches.Count > 0
                        ? result.FilePatches.Select(p => new { path = p.RelativePath, kind = "patch" }).ToArray()
                        : null
                }, ct).ConfigureAwait(false);
            }

            var toolCallRecord = new AgentToolCallRecord(
                0,
                sessionState.SessionId,
                toolCall.Name,
                toolCall.Input.GetRawText(),
                result.Output,
                result.Success,
                sessionState.LastToolDurationMs,
                DateTime.UtcNow);
            await PersistTurnAsync(sessionState.SessionId, turn, turns[^1], toolCallRecord, ct).ConfigureAwait(false);

            var tool = _registry.TryGet(toolCall.Name);
            var toolIsReadOnly = tool?.IsReadOnly == true;
            var readOnlyBeforeTool = consecutiveReadOnlyTools;
            var (afterToolDecision, consecutiveReadOnlyToolsAfter) = FSharpAlgorithmsBridge.DecideAfterTool(
                isGeneration,
                readOnlyBeforeTool,
                _options.MaxInvestigationToolsBeforeWriteNudge,
                allPatches,
                request.TargetRelativePaths,
                toolIsReadOnly);
            consecutiveReadOnlyTools = consecutiveReadOnlyToolsAfter;

            var toolOutput = result.Success ? result.Output : $"ERROR: {result.Output}";
            if (toolOutput.Length > _options.MaxToolResultChars)
                toolOutput = toolOutput[.._options.MaxToolResultChars] + "\n...[truncated]...";

            turns.Add(new AgentConversationTurn("tool", $"[{result.ToolName}] {toolOutput}", DateTime.UtcNow));
            trace.Add($"turn_{turn}:tool:{result.ToolName}:success={result.Success}");
            if (_hermesMemory is not null && !string.IsNullOrWhiteSpace(requestFingerprint))
            {
                await _hermesMemory.SyncAfterToolAsync(
                    new HermesTurnContext(request.RunId, requestFingerprint, stage, request.LastErrors),
                    result.ToolName,
                    toolOutput,
                    result.Success,
                    ct).ConfigureAwait(false);
            }

            await EmitStepFinishAsync(request.RunId, sessionState.SessionId, turn, "tool_calls", ct).ConfigureAwait(false);

            if (afterToolDecision == FSharpTurn.AfterToolDecision.InvestigationNudge)
            {
                turns.Add(new AgentConversationTurn(
                    "system",
                    "Investigation limit reached. Stop glob/grep/read_file. Call write_file NOW with full file content for each TARGET FILE.",
                    DateTime.UtcNow));
            }
        }

        var finalResult = new AgentSessionResult(
            allPatches.Count > 0,
            "Max turns reached",
            FilterPatches(allPatches, request.TargetRelativePaths),
            maxTurns,
            trace);
        await EmitSessionEndAsync(request, sessionState, ct).ConfigureAwait(false);
        _logger.LogInformation("Agent session completed: {Summary}", AgentRuntimeTelemetry.FormatSummary(request.Mode, finalResult));
        return finalResult;
    }

    private async Task EmitSessionEndAsync(AgentSessionRunRequest request, AgentSessionState sessionState, CancellationToken ct)
    {
        if (_lifecycle is null)
            return;

        await _lifecycle.RunAsync(AgentHookKind.SessionEnd, new HookContext
        {
            RunId = request.RunId,
            SessionId = sessionState.SessionId,
            WorkspaceRoot = request.Workspace.HostPath
        }, ct).ConfigureAwait(false);
    }

    private List<AgentConversationTurn> BuildInitialTurns(AgentSessionRunRequest request, bool isGeneration)
    {
        var promptRegistry = BuildPromptRegistry(request);
        var varContext = BuildVarContext(request, isGeneration);
        var useTemplates = _promptTemplates is not null;
        string initialUser;
        if (!isGeneration && request.Fim is not null && _fimBuilder is not null)
        {
            var fimPrompt = new FimPrompt(
                request.Fim.RelativePath,
                request.Fim.Prefix,
                request.Fim.Suffix,
                request.Fim.HoleContent,
                request.Fim.HoleStartLine,
                request.Fim.HoleEndLine);
            initialUser =
                $"FIM REPAIR MODE for {request.Fim.RelativePath} (lines {request.Fim.HoleStartLine}-{request.Fim.HoleEndLine}).\n" +
                $"{request.Objective}\n\n" +
                _fimBuilder.FormatLlmPrompt(fimPrompt) +
                "\n\nReturn ONLY the replacement code for <|fim_hole|>. Use write_file or apply_patch after producing the fill.";
        }
        else
        {
            initialUser = isGeneration
                ? AgentPromptBuilder.BuildGenerationObjective(
                    request.Objective,
                    request.Plan,
                    request.TargetRelativePaths ?? Array.Empty<string>(),
                    promptRegistry,
                    _promptVars,
                    varContext,
                    useInstructionTemplate: useTemplates)
                : AgentPromptBuilder.BuildUserObjective(
                    request.Objective,
                    request.Plan,
                    request.BuildLog,
                    promptRegistry,
                    _promptVars,
                    varContext,
                    request.ContextFragments,
                    useInstructionTemplate: useTemplates);
        }

        return [new AgentConversationTurn("user", initialUser, DateTime.UtcNow)];
    }

    private IAgentToolRegistry BuildPromptRegistry(AgentSessionRunRequest request) =>
        request.AllowedTools is { Count: > 0 } allowed
            ? new FilteredAgentToolRegistry(_registry, allowed)
            : _registry;

    private BuiltinPromptVarContext BuildVarContext(
        AgentSessionRunRequest request,
        bool isGeneration,
        AgentSessionState? sessionState = null) =>
        new()
        {
            Plan = request.Plan,
            WorkspaceHostPath = request.Workspace.HostPath,
            BuildLog = request.BuildLog,
            RunId = request.RunId,
            Stage = request.PromptStage ?? (isGeneration
                ? BuiltinPromptStage.Generating
                : BuiltinPromptStage.Repairing),
            RepairAttempt = request.RepairAttempt,
            LastErrors = request.LastErrors ?? Array.Empty<string>(),
            ManifestFiles = request.ManifestFiles
                           ?? request.TargetRelativePaths
                           ?? request.WorkingFiles.Select(f => f.RelativePath).Take(48).ToArray(),
            JitLibr4Context = sessionState?.ActiveLibr4Context,
            ActivatedSkillNames = sessionState?.ActivatedSkills.ToArray() ?? Array.Empty<string>()
        };

    private async Task MarkSessionCompletedAsync(string sessionId, CancellationToken ct)
    {
        if (!_options.EnableSessionPersistence || _sessionStore is null)
            return;

        var session = await _sessionStore.GetSessionAsync(sessionId, ct).ConfigureAwait(false);
        if (session is null)
            return;

        await _sessionStore.UpdateSessionAsync(session with
        {
            Status = "completed",
            LastStepAtUtc = DateTime.UtcNow
        }, ct).ConfigureAwait(false);
    }

    private async Task PersistTurnAsync(
        string sessionId,
        int step,
        AgentConversationTurn turn,
        AgentToolCallRecord? toolCall,
        CancellationToken ct)
    {
        if (!_options.EnableSessionPersistence || _resumeService is null)
            return;
        await _resumeService.SaveTurnAsync(sessionId, step, turn, toolCall, ct).ConfigureAwait(false);
    }

    private async Task InjectDelegationNotificationsAsync(
        Guid? runId,
        List<AgentConversationTurn> turns,
        CancellationToken ct)
    {
        if (_delegation is null || runId is not Guid id)
            return;

        while (true)
        {
            var notification = await _delegation.TryDequeueNotificationAsync(id, ct).ConfigureAwait(false);
            if (notification is null)
                break;

            turns.Add(new AgentConversationTurn(
                "system",
                BuildDelegationResultsSection(notification),
                DateTime.UtcNow));
        }
    }

    private static string BuildDelegationResultsSection(DelegationNotification notification) =>
        DelegationPromptFormatter.FormatResultsSection(notification);

    private async Task EmitStepStartAsync(Guid? runId, string sessionId, int step, CancellationToken ct)
    {
        if (runId is not Guid id)
            return;
        if (_rollout is not null && _options.EnableRolloutRecorder)
            await _rollout.RecordStepStartAsync(id, sessionId, step, ct).ConfigureAwait(false);
        if (_ndjson is not null && _options.EnableNdjsonEvents)
            await _ndjson.WriteAsync(id, new { type = "step_start", sessionId, stepNumber = step, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, ct).ConfigureAwait(false);
    }

    private async Task EmitStepFinishAsync(Guid? runId, string sessionId, int step, string reason, CancellationToken ct)
    {
        if (runId is not Guid id)
            return;
        if (_rollout is not null && _options.EnableRolloutRecorder)
            await _rollout.RecordStepFinishAsync(id, sessionId, step, reason, ct: ct).ConfigureAwait(false);
        if (_ndjson is not null && _options.EnableNdjsonEvents)
            await _ndjson.WriteAsync(id, new { type = "step_finish", sessionId, stepNumber = step, finishReason = reason, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, ct).ConfigureAwait(false);
    }

    private async Task EmitErrorAsync(Guid? runId, string sessionId, string message, CancellationToken ct)
    {
        if (runId is not Guid id)
            return;
        if (_rollout is not null && _options.EnableRolloutRecorder)
            await _rollout.RecordErrorAsync(id, sessionId, message, ct).ConfigureAwait(false);
        if (_ndjson is not null && _options.EnableNdjsonEvents)
            await _ndjson.WriteAsync(id, new { type = "error", sessionId, message, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, ct).ConfigureAwait(false);
    }

    private static IReadOnlyList<GeneratedFile> FilterPatches(
        Dictionary<string, GeneratedFile> patches,
        IReadOnlyList<string>? targetPaths)
    {
        return FSharpAlgorithmsBridge.FilterSessionPatches(patches, targetPaths);
    }

    private async Task<string> GenerateAgentCompletionAsync(
        string prompt,
        string systemPrompt,
        string stage,
        AgentSessionRunRequest request,
        CancellationToken ct)
    {
        var role = request.SubagentRole ?? AgentModelRoleNames.FromPipelineStage(stage);
        var route = _modelRouter.Route(role, request.ModelOverride);
        var timeoutSeconds = Math.Clamp(_options.BashTimeoutSeconds / 2, 60, 300);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var budgetedPrompt = PromptPipelinePolicy.ApplyInputBudget(stage, prompt);

        var stageReq = _providerMatrix.GetStageRequirements(stage)
                         ?? new StageModelRequirement(stage, false, false, false, 8000, 2048, 0.01);
        var routing = _providerMatrix.RouteStage(stage, stageReq);
        var provider = _providerMatrix.GetProvider(routing.ProviderId);
        var estimatedTokens = LlmCostEstimator.EstimateRequestTokens(
            budgetedPrompt,
            systemPrompt,
            stageReq.MinOutputTokens);
        var estimatedCostUsd = LlmCostEstimator.EstimateCostUsd(
            estimatedTokens,
            provider?.CostPer1kTokens ?? 0);

        if (request.RunId is Guid runId && _budget is not null)
        {
            _fleetScheduler?.RaiseImplementerBudgetPressure(runId, request.TenantUserId);

            var reservation = await _budget.TryConsumeAsync(
                runId,
                stage,
                estimatedTokens,
                estimatedCostUsd,
                ct).ConfigureAwait(false);
            if (!reservation.Granted)
            {
                _logger.LogWarning(
                    "Budget denied for run {RunId} stage {Stage}: {Reason}",
                    runId,
                    stage,
                    reservation.DenialReason);
                throw new BudgetExceededException(reservation.DenialReason ?? "budget_denied");
            }
        }

        Exception? lastError = null;
        foreach (var model in route.AllModels)
        {
            if (_modelRouter.IsRoleModelCircuitOpen(role, model))
            {
                _logger.LogWarning("Skipping model {Model} for role {Role} вЂ” role circuit open", model, role);
                continue;
            }

            try
            {
                var completionTask = Task.Run(async () =>
                {
                    using var prefScope = string.IsNullOrWhiteSpace(LlmCallPreferenceContext.CurrentPreferences?.ModelOverride)
                        ? LlmCallPreferenceContext.Activate(new LlmCallPreferences(model))
                        : null;
                    using var _ = AICallCancellationScope.Push(linkedCts.Token);
                    return await _ai.GenerateCompletionAsync(budgetedPrompt, systemPrompt, model).ConfigureAwait(false);
                }, linkedCts.Token);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), linkedCts.Token);
                var finished = await Task.WhenAny(completionTask, timeoutTask).ConfigureAwait(false);
                if (finished != completionTask)
                    throw new TimeoutException($"Agent session LLM call exceeded timeout of {timeoutSeconds}s.");

                linkedCts.Cancel();
                var result = await completionTask.ConfigureAwait(false);
                _modelRouter.RecordRoleModelSuccess(role, model);

                if (request.RunId is Guid costRunId && _costTracker is not null)
                {
                    var actualTokens = LlmCostEstimator.EstimateRequestTokens(
                        budgetedPrompt + result,
                        systemPrompt,
                        stageReq.MinOutputTokens);
                    var actualCostUsd = LlmCostEstimator.EstimateCostUsd(
                        actualTokens,
                        provider?.CostPer1kTokens ?? 0);
                    await _costTracker.RecordAsync(
                        costRunId,
                        routing.ProviderId,
                        stage,
                        model,
                        actualTokens,
                        actualCostUsd,
                        ct).ConfigureAwait(false);
                }

                return result;
            }
            catch (LlmCircuitOpenException ex)
            {
                lastError = ex;
                _modelRouter.RecordRoleModelFailure(role, model);
                _logger.LogWarning(ex, "Provider circuit open while calling model {Model} for role {Role}", model, role);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
                _modelRouter.RecordRoleModelFailure(role, model);
                _logger.LogWarning(ex, "Model {Model} failed for role {Role}; trying fallback if available", model, role);
            }
        }

        throw lastError ?? new InvalidOperationException($"no_eligible_model_for_role:{role}");
    }
}
