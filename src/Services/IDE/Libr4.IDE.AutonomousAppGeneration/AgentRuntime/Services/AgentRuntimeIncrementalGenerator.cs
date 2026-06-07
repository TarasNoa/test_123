using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.AutonomousAppGeneration.Agents;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Services;

public interface IAgentRuntimeIncrementalGenerator
{
    Task<GenerationPhaseBatchResult?> RunPhaseAsync(
        AppGenerationOrchestrator orchestrator,
        AgentPhase phase,
        GenerationPlan plan,
        AgentOrchestrationOptions options,
        Func<AppGenerationOrchestrator, CancellationToken, Task> saveOrchestratorAsync,
        object workspaceLock,
        PlannedFilePathRegistry? pathRegistry,
        CancellationToken ct);
}

public sealed class AgentRuntimeIncrementalGenerator : IAgentRuntimeIncrementalGenerator
{
    private readonly IAgentSession _session;
    private readonly GenerationWorkspaceStore _workspaceStore;
    private readonly GenerationWorkspaceAccessor _generationAccessor;
    private readonly IFeatureBatchHandoffCoordinator? _handoff;
    private readonly IRepoGraphBuilder? _repoGraphBuilder;
    private readonly RepoGraphOptions _repoGraphOptions;
    private readonly AgentRuntimeOptions _options;
    private readonly ILogger<AgentRuntimeIncrementalGenerator> _logger;

    public AgentRuntimeIncrementalGenerator(
        IAgentSession session,
        GenerationWorkspaceStore workspaceStore,
        GenerationWorkspaceAccessor generationAccessor,
        IOptions<AgentRuntimeOptions> options,
        ILogger<AgentRuntimeIncrementalGenerator> logger,
        IFeatureBatchHandoffCoordinator? handoff = null,
        IRepoGraphBuilder? repoGraphBuilder = null,
        IOptions<RepoGraphOptions>? repoGraphOptions = null)
    {
        _session = session;
        _workspaceStore = workspaceStore;
        _generationAccessor = generationAccessor;
        _handoff = handoff;
        _repoGraphBuilder = repoGraphBuilder;
        _repoGraphOptions = repoGraphOptions?.Value ?? new RepoGraphOptions();
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GenerationPhaseBatchResult?> RunPhaseAsync(
        AppGenerationOrchestrator orchestrator,
        AgentPhase phase,
        GenerationPlan plan,
        AgentOrchestrationOptions options,
        Func<AppGenerationOrchestrator, CancellationToken, Task> saveOrchestratorAsync,
        object workspaceLock,
        PlannedFilePathRegistry? pathRegistry,
        CancellationToken ct)
    {
        if (!_options.UseAgentRuntimeGeneration)
            return null;

        var allTasks = MultiAgentIncrementalManifest.CreateFileScopedTasks(
            phase,
            plan,
            options,
            pathRegistry,
            _repoGraphBuilder,
            Options.Create(_repoGraphOptions));
        if (allTasks.Count == 0)
        {
            _logger.LogWarning(
                "[AutoGen {Id}] Agent generation phase '{Phase}' has zero file tasks",
                orchestrator.Id,
                phase);
            return null;
        }

        List<AgentTask> tasks;
        List<AgentTask> skippedTasks;
        lock (workspaceLock)
        {
            (tasks, skippedTasks) = IncrementalFileTaskPlanner.PartitionByExistingWorkspace(
                allTasks,
                orchestrator.Files,
                options,
                pathRegistry);
        }

        _logger.LogInformation(
            "[AutoGen {Id}] Agent generation phase '{Phase}': {LlmTasks} task(s), skipped={Skipped}",
            orchestrator.Id,
            phase,
            tasks.Count,
            skippedTasks.Count);

        var phasePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skipped in skippedTasks)
        {
            foreach (var path in skipped.Context.TargetRelativePaths)
                phasePaths.Add(StackArtifactCompleteness.SanitizeRelativePath(path));
        }

        Guid workspaceId;
        lock (workspaceLock)
            workspaceId = _workspaceStore.Create(orchestrator.Files);

        try
        {
            string? handoffPrefix = null;
            if (phase == AgentPhase.Frontend && _handoff is not null)
                handoffPrefix = await _handoff.BuildFrontendHandoffPrefixAsync(orchestrator.Id, ct).ConfigureAwait(false);

            foreach (var task in tasks)
            {
                ct.ThrowIfCancellationRequested();
                var targets = task.Context.TargetRelativePaths;
                if (targets.Length == 0)
                    continue;

                List<DomainGeneratedFile> working;
                lock (workspaceLock)
                {
                    _workspaceStore.SyncFromFiles(workspaceId, orchestrator.Files);
                    working = orchestrator.Files
                        .Select(f => new DomainGeneratedFile(f.RelativePath, f.Language, f.Content))
                        .ToList();
                }

                if (!_generationAccessor.TryGetWorkspace(workspaceId, out var wsContext))
                {
                    _logger.LogWarning("[AutoGen {Id}] Generation workspace {Ws} missing", orchestrator.Id, workspaceId);
                    continue;
                }

                _logger.LogInformation(
                    "[AutoGen {Id}] Agent generating [{Targets}] — {Desc}",
                    orchestrator.Id,
                    string.Join(", ", targets),
                    task.Description);

                var objective = string.IsNullOrWhiteSpace(handoffPrefix)
                    ? task.Description
                    : handoffPrefix + task.Description;
                handoffPrefix = null;

                var result = await _session.RunAsync(
                    new AgentSessionRunRequest(
                        objective,
                        wsContext,
                        working,
                        plan,
                        _generationAccessor,
                        AgentSessionMode.Generation,
                        TargetRelativePaths: targets,
                        RunId: orchestrator.Id,
                        TenantUserId: orchestrator.TenantId,
                        PromptStage: BuiltinPromptStage.Generating,
                        ManifestFiles: pathRegistry?.AllowedPaths.Take(64).ToArray()
                                       ?? targets),
                    ct).ConfigureAwait(false);

                if (result.Patches.Count == 0)
                {
                    var minChars = Math.Clamp(options.MinCharsToSkipIncrementalTask, 20, 8_000);
                    lock (workspaceLock)
                    {
                        var satisfied = targets.All(path =>
                            IncrementalFileTaskPlanner.TryGetExistingCompleteTarget(
                                path,
                                orchestrator.Files,
                                minChars,
                                out _));
                        if (satisfied)
                        {
                            foreach (var path in targets)
                                phasePaths.Add(StackArtifactCompleteness.SanitizeRelativePath(path));
                            continue;
                        }
                    }

                    _logger.LogWarning(
                        "[AutoGen {Id}] Agent task {TaskId} produced no files for [{Targets}] ({Summary})",
                        orchestrator.Id,
                        task.Id,
                        string.Join(", ", targets),
                        result.Summary);
                    continue;
                }

                var filtered = pathRegistry is not null && options.RejectUnplannedGeneratedPaths
                    ? pathRegistry.AcceptOnlyPlanned(result.Patches, targets)
                    : MultiAgentGenerationContext.FilterParsedToTargets(result.Patches, targets);

                if (filtered.Count == 0)
                    continue;

                var added = 0;
                lock (workspaceLock)
                {
                    foreach (var file in filtered)
                    {
                        var repaired = StackArtifactCompleteness.RepairGeneratedFile(file);
                        if (repaired is null)
                            continue;

                        orchestrator.UpsertFile(repaired);
                        phasePaths.Add(repaired.RelativePath);
                        added++;
                    }
                }

                await saveOrchestratorAsync(orchestrator, ct).ConfigureAwait(false);
                _logger.LogInformation(
                    "[AutoGen {Id}] Agent save after task {TaskId}: +{Added} file(s), turns={Turns}, workspace={Total}",
                    orchestrator.Id,
                    task.Id,
                    added,
                    result.TurnsUsed,
                    orchestrator.Files.Count);
            }
        }
        finally
        {
            _workspaceStore.Dispose(workspaceId);
        }

        List<DomainGeneratedFile> collected;
        lock (workspaceLock)
        {
            collected = IncrementalFileTaskPlanner.CollectPhaseWorkspaceFiles(
                phase,
                orchestrator.Files,
                phasePaths);
        }

        collected = PhaseArtifactPathNormalizer.NormalizeForPhase(phase, collected, plan);
        if (phase == AgentPhase.Backend && _handoff is not null && collected.Count > 0)
            await _handoff.SendBackendToFrontendAsync(orchestrator.Id, collected, ct).ConfigureAwait(false);

        if (collected.Count == 0)
        {
            _logger.LogWarning(
                "[AutoGen {Id}] Agent generation phase '{Phase}' finished with no artifacts",
                orchestrator.Id,
                phase);
            return null;
        }

        return new GenerationPhaseBatchResult(phase.ToString().ToLowerInvariant(), collected);
    }
}
