using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Orchestrates parallel subagent execution with optional lightweight review.
/// </summary>
public class SubagentOrchestrator
{
    private readonly IAgent _implementerAgent;
    private readonly IAgent _specReviewerAgent;
    private readonly IAgent _codeQualityReviewerAgent;
    private readonly IAgentSpawner? _spawner;
    private readonly AgentOrchestrationOptions _options;
    private readonly ILogger _logger;
    private readonly int _maxConcurrency;
    private readonly int _maxSubtaskDepth;

    public SubagentOrchestrator(
        IAgent implementerAgent,
        IAgent specReviewerAgent,
        IAgent codeQualityReviewerAgent,
        ILogger logger,
        int maxConcurrency = 5,
        int maxSubtaskDepth = 2,
        IAgentSpawner? spawner = null,
        AgentOrchestrationOptions? options = null)
    {
        _implementerAgent = implementerAgent;
        _specReviewerAgent = specReviewerAgent;
        _codeQualityReviewerAgent = codeQualityReviewerAgent;
        _spawner = spawner;
        _options = options ?? new AgentOrchestrationOptions();
        _logger = logger;
        _maxConcurrency = Math.Max(1, _options.MaxConcurrentTasks > 0 ? _options.MaxConcurrentTasks : maxConcurrency);
        _maxSubtaskDepth = Math.Clamp(maxSubtaskDepth, 0, 5);
    }

    public async Task<OrchestrationResult> ExecuteParallelAsync(
        List<AgentTask> tasks,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TaskResult>();
        var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Starting parallel subagent orchestration for {TaskCount} tasks", tasks.Count);

        var parallelTasks = tasks.Select(async task =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await ExecuteTaskWithReviewAsync(task, cancellationToken, depth: 0);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var taskResults = await Task.WhenAll(parallelTasks);
        results.AddRange(taskResults.Where(r => r != null)!);

        stopwatch.Stop();
        _logger.LogInformation(
            "Parallel subagent orchestration completed in {ElapsedMs}ms. Success: {SuccessCount}/{TotalCount}",
            stopwatch.ElapsedMilliseconds,
            results.Count(r => r.IsSuccess),
            results.Count);

        return new OrchestrationResult
        {
            Results = results,
            TotalDuration = stopwatch.Elapsed,
            SuccessCount = results.Count(r => r.IsSuccess),
            FailureCount = results.Count(r => !r.IsSuccess)
        };
    }

    private async Task<TaskResult> ExecuteTaskWithReviewAsync(
        AgentTask task,
        CancellationToken cancellationToken,
        int depth)
    {
        var taskStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var reviewCount = 0;

        try
        {
            _logger.LogInformation("Executing task: {TaskId} - {TaskDescription}", task.Id, task.Description);

            var nestedResults = await ExecuteNestedSubtasksAsync(task, cancellationToken, depth);
            ClearSubtasksAfterNestedDelegation(task);
            var nestedFailure = nestedResults.FirstOrDefault(r => !r.IsSuccess);
            if (nestedFailure is not null)
            {
                taskStopwatch.Stop();
                return new TaskResult
                {
                    TaskId = task.Id,
                    ParentTaskId = task.ParentTaskId,
                    IsSuccess = false,
                    Error = $"Nested subtask failed: {nestedFailure.TaskId} -> {nestedFailure.Error}",
                    ReviewCount = reviewCount,
                    NestedResults = nestedResults,
                    Duration = taskStopwatch.Elapsed
                };
            }

            var implementResult = await _implementerAgent.ExecuteAsync(task.Context);
            reviewCount++;

            implementResult = MergeNestedContent(implementResult, nestedResults);

            if (TryAcceptImplementerOutput(implementResult, out var acceptReason))
            {
                taskStopwatch.Stop();
                _logger.LogInformation(
                    "Task {TaskId} accepted via {Reason} in {ElapsedMs}ms ({ReviewCount} reviews)",
                    task.Id, acceptReason, taskStopwatch.ElapsedMilliseconds, reviewCount);

                return new TaskResult
                {
                    TaskId = task.Id,
                    ParentTaskId = task.ParentTaskId,
                    IsSuccess = true,
                    Result = implementResult,
                    ReviewCount = reviewCount,
                    NestedResults = nestedResults,
                    Duration = taskStopwatch.Elapsed
                };
            }

            if (_options.MaxLlmReviewRounds <= 0)
            {
                taskStopwatch.Stop();
                var ok = implementResult.IsSuccess && !string.IsNullOrWhiteSpace(implementResult.Content);
                return new TaskResult
                {
                    TaskId = task.Id,
                    ParentTaskId = task.ParentTaskId,
                    IsSuccess = ok,
                    Result = implementResult,
                    Error = ok ? null : "Empty implementer output",
                    ReviewCount = reviewCount,
                    NestedResults = nestedResults,
                    Duration = taskStopwatch.Elapsed
                };
            }

            var specReview = await _specReviewerAgent.ExecuteAsync(new AgentContext(task, implementResult));
            reviewCount++;

            if (!specReview.IsApproved && _options.MaxLlmReviewRounds >= 1)
            {
                implementResult = await _implementerAgent.ExecuteAsync(new AgentContext(task, specReview.Feedback));
                reviewCount++;
                if (TryAcceptImplementerOutput(implementResult, out _))
                {
                    return BuildSuccess(task, implementResult, reviewCount, nestedResults, taskStopwatch);
                }

                specReview = await _specReviewerAgent.ExecuteAsync(new AgentContext(task, implementResult));
                reviewCount++;
                if (!specReview.IsApproved)
                {
                    return BuildFailure(task, "Spec compliance review failed after fix attempt", reviewCount, nestedResults, taskStopwatch);
                }
            }

            if (_options.MaxLlmReviewRounds >= 2)
            {
                var qualityReview = await _codeQualityReviewerAgent.ExecuteAsync(new AgentContext(task, implementResult));
                reviewCount++;
                if (!qualityReview.IsApproved)
                {
                    implementResult = await _implementerAgent.ExecuteAsync(new AgentContext(task, qualityReview.Feedback));
                    reviewCount++;
                    qualityReview = await _codeQualityReviewerAgent.ExecuteAsync(new AgentContext(task, implementResult));
                    reviewCount++;
                    if (!qualityReview.IsApproved)
                    {
                        return BuildFailure(task, "Code quality review failed after fix attempt", reviewCount, nestedResults, taskStopwatch);
                    }
                }
            }

            return BuildSuccess(task, implementResult, reviewCount, nestedResults, taskStopwatch);
        }
        catch (Exception ex)
        {
            taskStopwatch.Stop();
            _logger.LogError(ex, "Task {TaskId} failed with exception", task.Id);
            return new TaskResult
            {
                TaskId = task.Id,
                ParentTaskId = task.ParentTaskId,
                IsSuccess = false,
                Error = ex.Message,
                ReviewCount = reviewCount,
                Duration = taskStopwatch.Elapsed
            };
        }
    }

    private static void ClearSubtasksAfterNestedDelegation(AgentTask task)
    {
        task.Subtasks.Clear();
        if (task.Context.Task is not null)
            task.Context.Task.Subtasks.Clear();
    }

    private bool TryAcceptImplementerOutput(AgentResult implementResult, out string reason)
    {
        if (_options.SkipLlmReviewWhenParseableFiles
            && AgentGeneratedFileParser.HasParseableFiles(implementResult.Content))
        {
            reason = "parseable_files_fast_path";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static AgentResult MergeNestedContent(AgentResult parent, IReadOnlyList<TaskResult> nested)
    {
        if (nested.Count == 0)
            return parent;

        var parts = new List<string>();
        foreach (var n in nested.Where(r => r.IsSuccess && r.Result is not null))
            parts.Add(n.Result!.Content);

        if (!string.IsNullOrWhiteSpace(parent.Content))
            parts.Add(parent.Content);

        return new AgentResult
        {
            IsSuccess = parent.IsSuccess || parts.Count > 0,
            Content = string.Join("\n", parts),
            SuggestedSubtasks = parent.SuggestedSubtasks
        };
    }

    private async Task<List<TaskResult>> ExecuteNestedSubtasksAsync(
        AgentTask parentTask,
        CancellationToken cancellationToken,
        int depth)
    {
        var nested = new List<TaskResult>();
        if (depth > _maxSubtaskDepth)
            return nested;

        var candidateSubtasks = new List<AgentTask>();
        if (parentTask.Subtasks.Count > 0)
            candidateSubtasks.AddRange(parentTask.Subtasks);
        if (parentTask.Context.Task?.Subtasks.Count > 0 == true)
            candidateSubtasks.AddRange(parentTask.Context.Task.Subtasks);

        candidateSubtasks = candidateSubtasks
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .ToList();

        if (candidateSubtasks.Count == 0)
            return nested;

        if (_options.RunNestedSubtasksInParallel && candidateSubtasks.Count > 1)
        {
            var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
            var parallel = candidateSubtasks.Select(async sub =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await ExecuteRoleSubtaskFastAsync(parentTask, sub, cancellationToken, depth);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            nested.AddRange(await Task.WhenAll(parallel));
            return nested;
        }

        foreach (var sub in candidateSubtasks)
        {
            var subResult = await ExecuteRoleSubtaskFastAsync(parentTask, sub, cancellationToken, depth);
            nested.Add(subResult);
            if (!subResult.IsSuccess)
                break;
        }

        return nested;
    }

    private async Task<TaskResult> ExecuteRoleSubtaskFastAsync(
        AgentTask parentTask,
        AgentTask sub,
        CancellationToken cancellationToken,
        int depth)
    {
        sub.ParentTaskId ??= parentTask.Id;
        if (sub.Context.Task is null)
            sub.Context.Task = sub;
        sub.Context.ApplicationName = string.IsNullOrWhiteSpace(sub.Context.ApplicationName)
            ? parentTask.Context.ApplicationName
            : sub.Context.ApplicationName;
        if (string.IsNullOrWhiteSpace(sub.Context.Description))
            sub.Context.Description = sub.Description;

        var role = sub.Context.TechStack;
        if (_spawner is not null && !string.IsNullOrWhiteSpace(role) && role.Contains('-'))
        {
            _logger.LogInformation(
                "Delegating nested subtask {SubId} to spawner role={Role}",
                sub.Id,
                role);

            try
            {
                var spawned = await _spawner.SpawnAndExecuteAsync(role, sub.Context, cancellationToken);
                if (TryAcceptImplementerOutput(spawned, out _))
                {
                    return new TaskResult
                    {
                        TaskId = sub.Id,
                        ParentTaskId = parentTask.Id,
                        IsSuccess = true,
                        Result = spawned,
                        ReviewCount = 0,
                        Duration = TimeSpan.Zero
                    };
                }

                if (spawned.IsSuccess && !string.IsNullOrWhiteSpace(spawned.Content))
                {
                    return new TaskResult
                    {
                        TaskId = sub.Id,
                        ParentTaskId = parentTask.Id,
                        IsSuccess = true,
                        Result = spawned,
                        ReviewCount = 0,
                        Duration = TimeSpan.Zero
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Spawner failed for role {Role}, falling back to implementer", role);
            }
        }

        return await ExecuteTaskWithReviewAsync(sub, cancellationToken, depth + 1);
    }

    private static TaskResult BuildSuccess(
        AgentTask task,
        AgentResult implementResult,
        int reviewCount,
        IReadOnlyList<TaskResult> nestedResults,
        System.Diagnostics.Stopwatch sw)
    {
        sw.Stop();
        return new TaskResult
        {
            TaskId = task.Id,
            ParentTaskId = task.ParentTaskId,
            IsSuccess = true,
            Result = implementResult,
            ReviewCount = reviewCount,
            NestedResults = nestedResults,
            Duration = sw.Elapsed
        };
    }

    private static TaskResult BuildFailure(
        AgentTask task,
        string error,
        int reviewCount,
        IReadOnlyList<TaskResult> nestedResults,
        System.Diagnostics.Stopwatch sw)
    {
        sw.Stop();
        return new TaskResult
        {
            TaskId = task.Id,
            ParentTaskId = task.ParentTaskId,
            IsSuccess = false,
            Error = error,
            ReviewCount = reviewCount,
            NestedResults = nestedResults,
            Duration = sw.Elapsed
        };
    }

    public async Task<OrchestrationResult> ExecuteSequentialAsync(
        List<AgentTask> tasks,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TaskResult>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Starting sequential subagent orchestration for {TaskCount} tasks", tasks.Count);

        foreach (var task in tasks)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await ExecuteTaskWithReviewAsync(task, cancellationToken, depth: 0);
            results.Add(result);
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "Sequential subagent orchestration completed in {ElapsedMs}ms. Success: {SuccessCount}/{TotalCount}",
            stopwatch.ElapsedMilliseconds,
            results.Count(r => r.IsSuccess),
            results.Count);

        return new OrchestrationResult
        {
            Results = results,
            TotalDuration = stopwatch.Elapsed,
            SuccessCount = results.Count(r => r.IsSuccess),
            FailureCount = results.Count(r => !r.IsSuccess)
        };
    }
}

public class OrchestrationResult
{
    public List<TaskResult> Results { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate => Results.Count > 0 ? (double)SuccessCount / Results.Count : 0;
}

public class TaskResult
{
    public string TaskId { get; set; } = string.Empty;
    public string? ParentTaskId { get; set; }
    public bool IsSuccess { get; set; }
    public AgentResult? Result { get; set; }
    public string? Error { get; set; }
    public int ReviewCount { get; set; }
    public IReadOnlyList<TaskResult> NestedResults { get; set; } = Array.Empty<TaskResult>();
    public TimeSpan Duration { get; set; }
}
