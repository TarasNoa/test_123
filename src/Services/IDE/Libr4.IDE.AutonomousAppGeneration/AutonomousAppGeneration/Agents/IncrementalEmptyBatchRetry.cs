using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Retries incremental LLM tasks that return no parseable files (common on small codegen models).
/// </summary>
internal static class IncrementalEmptyBatchRetry
{
    private const string EmptyRetrySuffix =
        "\n\n[MANDATORY RETRY] The previous attempt returned no files. " +
        "Emit {\"files\":[{\"relativePath\":\"...\",\"content\":\"...\"}]} for every TARGET that is missing or incomplete. " +
        "Do NOT return {\"files\":[]} unless every target is fully implemented and consistent with the task.";

    public static async Task<OrchestrationResult> ExecuteSequentialWithRetriesAsync(
        SubagentOrchestrator orchestrator,
        IReadOnlyList<AgentTask> tasks,
        AppGenerationOrchestrator generationOrchestrator,
        object workspaceLock,
        AgentOrchestrationOptions options,
        ILogger logger,
        CancellationToken ct)
    {
        var results = new List<TaskResult>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var maxRetries = Math.Clamp(options.IncrementalEmptyBatchMaxRetries, 0, 4);
        var minChars = Math.Clamp(options.MinCharsToSkipIncrementalTask, 20, 8_000);

        foreach (var task in tasks)
        {
            if (ct.IsCancellationRequested)
                break;

            var batchResults = await ExecuteTaskWithRetriesAsync(
                orchestrator,
                task,
                generationOrchestrator,
                workspaceLock,
                maxRetries,
                minChars,
                logger,
                generationOrchestrator.Id,
                ct).ConfigureAwait(false);
            results.AddRange(batchResults);
        }

        stopwatch.Stop();
        return new OrchestrationResult
        {
            Results = results,
            TotalDuration = stopwatch.Elapsed,
            SuccessCount = results.Count(r => r.IsSuccess),
            FailureCount = results.Count(r => !r.IsSuccess)
        };
    }

    private static async Task<List<TaskResult>> ExecuteTaskWithRetriesAsync(
        SubagentOrchestrator orchestrator,
        AgentTask task,
        AppGenerationOrchestrator generationOrchestrator,
        object workspaceLock,
        int maxRetries,
        int minChars,
        ILogger logger,
        Guid runId,
        CancellationToken ct)
    {
        var results = new List<TaskResult>();
        var result = await orchestrator.ExecuteSingleTaskAsync(task, ct).ConfigureAwait(false);
        results.Add(result);

        if (TargetsSatisfied(task, generationOrchestrator, workspaceLock, minChars))
            return results;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (ct.IsCancellationRequested)
                break;

            var retryTask = AugmentForEmptyRetry(task, attempt);
            logger.LogWarning(
                "[AutoGen {Id}] Empty incremental batch for task {TaskId}; retry {Attempt}/{Max} targets=[{Targets}]",
                runId,
                task.Id,
                attempt,
                maxRetries,
                string.Join(", ", task.Context.TargetRelativePaths));

            result = await orchestrator.ExecuteSingleTaskAsync(retryTask, ct).ConfigureAwait(false);
            results.Add(result);

            if (TargetsSatisfied(task, generationOrchestrator, workspaceLock, minChars))
                return results;
        }

        if (task.Context.TargetRelativePaths.Length <= 1)
            return results;

        logger.LogWarning(
            "[AutoGen {Id}] Splitting empty batch task {TaskId} into {Count} single-file retry task(s)",
            runId,
            task.Id,
            task.Context.TargetRelativePaths.Length);

        foreach (var path in task.Context.TargetRelativePaths)
        {
            if (ct.IsCancellationRequested)
                break;

            var single = MultiAgentIncrementalManifest.CreateSinglePathRetryTask(task, path);
            result = await orchestrator.ExecuteSingleTaskAsync(single, ct).ConfigureAwait(false);
            results.Add(result);

            if (!TargetsSatisfied(single, generationOrchestrator, workspaceLock, minChars))
            {
                var splitRetry = AugmentForEmptyRetry(single, attempt: 99);
                result = await orchestrator.ExecuteSingleTaskAsync(splitRetry, ct).ConfigureAwait(false);
                results.Add(result);
            }
        }

        return results;
    }

    private static bool TargetsSatisfied(
        AgentTask task,
        AppGenerationOrchestrator orchestrator,
        object workspaceLock,
        int minChars)
    {
        lock (workspaceLock)
        {
            return task.Context.TargetRelativePaths.All(path =>
                IncrementalFileTaskPlanner.TryGetExistingCompleteTarget(
                    path,
                    orchestrator.Files,
                    minChars,
                    out _));
        }
    }

    private static AgentTask AugmentForEmptyRetry(AgentTask source, int attempt)
    {
        var retry = CloneTask(source);
        retry.Description = source.Description + EmptyRetrySuffix + $" (retry #{attempt})";
        retry.Context.Description = retry.Description;
        return retry;
    }

    private static AgentTask CloneTask(AgentTask source)
    {
        var ctx = new AgentContext
        {
            ApplicationName = source.Context.ApplicationName,
            Description = source.Context.Description,
            TechStack = source.Context.TechStack,
            TargetRelativePaths = source.Context.TargetRelativePaths.ToArray(),
            PlannedPhasePaths = source.Context.PlannedPhasePaths?.ToArray() ?? Array.Empty<string>(),
            ScopedOutputOnly = source.Context.ScopedOutputOnly,
            GeneratedFiles = source.Context.GeneratedFiles?.ToArray(),
            Task = null
        };

        return new AgentTask
        {
            Id = source.Id,
            Description = source.Description,
            Context = ctx
        };
    }
}
