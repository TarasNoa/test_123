using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Runs a generation phase with per-task persistence and workspace-aware prompts.
/// </summary>
public sealed class IncrementalMultiAgentPhaseRunner
{
    private readonly ILogger _logger;
    private readonly IRepoGraphBuilder? _repoGraphBuilder;
    private readonly IOptions<RepoGraphOptions>? _repoGraphOptions;

    public IncrementalMultiAgentPhaseRunner(
        ILogger logger,
        IRepoGraphBuilder? repoGraphBuilder = null,
        IOptions<RepoGraphOptions>? repoGraphOptions = null)
    {
        _logger = logger;
        _repoGraphBuilder = repoGraphBuilder;
        _repoGraphOptions = repoGraphOptions;
    }

    public async Task<GenerationPhaseBatchResult?> RunAsync(
        AppGenerationOrchestrator orchestrator,
        AgentPhase phase,
        SubagentOrchestrator subOrchestrator,
        GenerationPlan plan,
        AgentOrchestrationOptions options,
        Func<AppGenerationOrchestrator, CancellationToken, Task> saveOrchestratorAsync,
        object workspaceLock,
        PlannedFilePathRegistry? pathRegistry,
        CancellationToken ct)
    {
        var allTasks = MultiAgentIncrementalManifest.CreateFileScopedTasks(
            phase,
            plan,
            options,
            pathRegistry,
            _repoGraphBuilder,
            _repoGraphOptions);
        if (allTasks.Count == 0)
        {
            _logger.LogWarning(
                "[AutoGen {Id}] Incremental phase '{Phase}' has zero file tasks",
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

            if (skippedTasks.Count > 0)
            {
                _logger.LogInformation(
                    "[AutoGen {Id}] Incremental phase '{Phase}': skipping {Skipped}/{Total} task(s) — targets already in workspace",
                    orchestrator.Id,
                    phase,
                    skippedTasks.Count,
                    allTasks.Count);
            }
        }

        _logger.LogInformation(
            "[AutoGen {Id}] Incremental phase '{Phase}': {TaskCount} LLM task(s) (max concurrency {Concurrency})",
            orchestrator.Id,
            phase,
            tasks.Count,
            options.MaxConcurrentTasks);

        var phasePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skipped in skippedTasks)
        {
            foreach (var path in skipped.Context.TargetRelativePaths)
                phasePaths.Add(StackArtifactCompleteness.SanitizeRelativePath(path));
        }

        subOrchestrator.OnTaskStarting = task =>
        {
            lock (workspaceLock)
                MultiAgentGenerationContext.ApplyWorkspaceSnapshot(task, orchestrator.Files, options);
        };

        subOrchestrator.OnTaskCompletedAsync = async (task, result, token) =>
        {
            if (result.Result is null || string.IsNullOrWhiteSpace(result.Result.Content))
                return;

            var parsed = AgentGeneratedFileParser.TryParse(result.Result.Content);
            var filtered = pathRegistry is not null && options.RejectUnplannedGeneratedPaths
                ? pathRegistry.AcceptOnlyPlanned(parsed, task.Context.TargetRelativePaths)
                : MultiAgentGenerationContext.FilterParsedToTargets(parsed, task.Context.TargetRelativePaths);

            if (filtered.Count == 0)
            {
                var minChars = Math.Clamp(options.MinCharsToSkipIncrementalTask, 20, 8_000);
                lock (workspaceLock)
                {
                    var allSatisfied = task.Context.TargetRelativePaths.All(path =>
                        IncrementalFileTaskPlanner.TryGetExistingCompleteTarget(
                            path,
                            orchestrator.Files,
                            minChars,
                            out _));

                    if (allSatisfied)
                    {
                        foreach (var path in task.Context.TargetRelativePaths)
                            phasePaths.Add(StackArtifactCompleteness.SanitizeRelativePath(path));
                        return;
                    }
                }

                _logger.LogWarning(
                    "[AutoGen {Id}] Task {TaskId} produced no files for targets [{Targets}]",
                    orchestrator.Id,
                    task.Id,
                    string.Join(", ", task.Context.TargetRelativePaths));
                return;
            }

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

            await saveOrchestratorAsync(orchestrator, token).ConfigureAwait(false);
            _logger.LogInformation(
                "[AutoGen {Id}] Incremental save after task {TaskId}: +{Added} file(s), workspace total={Total}",
                orchestrator.Id,
                task.Id,
                added,
                orchestrator.Files.Count);
        };

        OrchestrationResult? phaseResult = null;
        if (tasks.Count > 0)
        {
            phaseResult = options.UseParallelTasksPerPhase
                ? await subOrchestrator.ExecuteParallelAsync(tasks, ct).ConfigureAwait(false)
                : options.IncrementalEmptyBatchMaxRetries > 0
                    ? await IncrementalEmptyBatchRetry.ExecuteSequentialWithRetriesAsync(
                        subOrchestrator,
                        tasks,
                        orchestrator,
                        workspaceLock,
                        options,
                        _logger,
                        ct).ConfigureAwait(false)
                    : await subOrchestrator.ExecuteSequentialAsync(tasks, ct).ConfigureAwait(false);
        }

        subOrchestrator.OnTaskStarting = null;
        subOrchestrator.OnTaskCompletedAsync = null;

        List<DomainGeneratedFile> collected;
        lock (workspaceLock)
        {
            collected = IncrementalFileTaskPlanner.CollectPhaseWorkspaceFiles(
                phase,
                orchestrator.Files,
                phasePaths);
        }

        if (collected.Count == 0 && phaseResult is not null)
            collected = MultiAgentArtifactCollector.CollectFiles(phaseResult);

        collected = PhaseArtifactPathNormalizer.NormalizeForPhase(phase, collected, plan);

        if (collected.Count == 0)
        {
            _logger.LogWarning(
                "[AutoGen {Id}] Incremental phase '{Phase}' finished with no artifacts (llm_tasks={LlmTasks}, success {Ok}/{Total})",
                orchestrator.Id,
                phase,
                tasks.Count,
                phaseResult?.SuccessCount ?? 0,
                phaseResult?.Results.Count ?? 0);
            return null;
        }

        return new GenerationPhaseBatchResult(phase.ToString().ToLowerInvariant(), collected);
    }
}
