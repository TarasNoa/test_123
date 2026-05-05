using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Orchestrates parallel subagent execution with two-stage review
/// Inspired by superpowers subagent-driven development pattern
/// </summary>
public class SubagentOrchestrator
{
    private readonly IAgent _implementerAgent;
    private readonly IAgent _specReviewerAgent;
    private readonly IAgent _codeQualityReviewerAgent;
    private readonly ILogger _logger;
    private readonly int _maxConcurrency;
    private readonly int _maxSubtaskDepth;

    public SubagentOrchestrator(
        IAgent implementerAgent,
        IAgent specReviewerAgent,
        IAgent codeQualityReviewerAgent,
        ILogger logger,
        int maxConcurrency = 5,
        int maxSubtaskDepth = 2)
    {
        _implementerAgent = implementerAgent;
        _specReviewerAgent = specReviewerAgent;
        _codeQualityReviewerAgent = codeQualityReviewerAgent;
        _logger = logger;
        _maxConcurrency = maxConcurrency;
        _maxSubtaskDepth = Math.Clamp(maxSubtaskDepth, 0, 5);
    }

    /// <summary>
    /// Execute tasks in parallel with two-stage review (spec compliance + code quality)
    /// </summary>
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
        results.AddRange(taskResults.Where(r => r != null));

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

    /// <summary>
    /// Execute a single task with two-stage review
    /// </summary>
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

            // Stage 1: Implementer subagent
            var implementResult = await _implementerAgent.ExecuteAsync(task.Context);
            reviewCount++;

            // Stage 2: Spec compliance review
            var specReviewContext = new AgentContext(task, implementResult);
            var specReview = await _specReviewerAgent.ExecuteAsync(specReviewContext);
            reviewCount++;

            if (!specReview.IsApproved)
            {
                _logger.LogInformation(
                    "Task {TaskId} spec review failed. Feedback: {Feedback}",
                    task.Id,
                    specReview.Feedback);

                // Fix spec gaps
                var fixContext = new AgentContext(task, specReview.Feedback);
                implementResult = await _implementerAgent.ExecuteAsync(fixContext);
                reviewCount++;

                // Re-review
                specReview = await _specReviewerAgent.ExecuteAsync(
                    new AgentContext(task, implementResult));
                reviewCount++;

                if (!specReview.IsApproved)
                {
                    _logger.LogWarning("Task {TaskId} spec re-review still failed", task.Id);
                    return new TaskResult
                    {
                        TaskId = task.Id,
                        IsSuccess = false,
                        Error = "Spec compliance review failed after fix attempt",
                        ReviewCount = reviewCount,
                        Duration = taskStopwatch.Elapsed
                    };
                }
            }

            // Stage 3: Code quality review
            var qualityReviewContext = new AgentContext(task, implementResult);
            var qualityReview = await _codeQualityReviewerAgent.ExecuteAsync(qualityReviewContext);
            reviewCount++;

            if (!qualityReview.IsApproved)
            {
                _logger.LogInformation(
                    "Task {TaskId} quality review failed. Feedback: {Feedback}",
                    task.Id,
                    qualityReview.Feedback);

                // Fix quality issues
                var fixContext = new AgentContext(task, qualityReview.Feedback);
                implementResult = await _implementerAgent.ExecuteAsync(fixContext);
                reviewCount++;

                // Re-review
                qualityReview = await _codeQualityReviewerAgent.ExecuteAsync(
                    new AgentContext(task, implementResult));
                reviewCount++;

                if (!qualityReview.IsApproved)
                {
                    _logger.LogWarning("Task {TaskId} quality re-review still failed", task.Id);
                    return new TaskResult
                    {
                        TaskId = task.Id,
                        IsSuccess = false,
                        Error = "Code quality review failed after fix attempt",
                        ReviewCount = reviewCount,
                        Duration = taskStopwatch.Elapsed
                    };
                }
            }

            var nestedResults = await ExecuteNestedSubtasksAsync(
                task,
                implementResult,
                cancellationToken,
                depth + 1);
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

            taskStopwatch.Stop();
            _logger.LogInformation(
                "Task {TaskId} completed successfully in {ElapsedMs}ms ({ReviewCount} reviews)",
                task.Id,
                taskStopwatch.ElapsedMilliseconds,
                reviewCount);

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

    private async Task<List<TaskResult>> ExecuteNestedSubtasksAsync(
        AgentTask parentTask,
        AgentResult implementResult,
        CancellationToken cancellationToken,
        int depth)
    {
        var nested = new List<TaskResult>();
        if (depth > _maxSubtaskDepth)
            return nested;

        var candidateSubtasks = new List<AgentTask>();
        if (parentTask.Subtasks.Count > 0)
            candidateSubtasks.AddRange(parentTask.Subtasks);
        if (implementResult.SuggestedSubtasks is { Count: > 0 })
            candidateSubtasks.AddRange(implementResult.SuggestedSubtasks);

        foreach (var sub in candidateSubtasks)
        {
            sub.ParentTaskId ??= parentTask.Id;
            if (sub.Context is null)
                sub.Context = new AgentContext();
            if (sub.Context.Task is null)
                sub.Context.Task = sub;
            var subResult = await ExecuteTaskWithReviewAsync(sub, cancellationToken, depth);
            nested.Add(subResult);
            if (!subResult.IsSuccess)
                break;
        }

        return nested;
    }

    /// <summary>
    /// Execute tasks sequentially (fallback for tightly coupled tasks)
    /// </summary>
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
            {
                break;
            }

            var result = await ExecuteTaskWithReviewAsync(task, cancellationToken, depth: 0);
            results.Add(result);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Task {TaskId} failed in sequential execution", task.Id);
                // Continue with remaining tasks
            }
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

/// <summary>
/// Orchestration result
/// </summary>
public class OrchestrationResult
{
    public List<TaskResult> Results { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate => Results.Count > 0 ? (double)SuccessCount / Results.Count : 0;
}

/// <summary>
/// Task result
/// </summary>
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
