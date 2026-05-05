namespace Libr4.Shared.Contracts.Manager;

/// <summary>
/// Represents an agent task or operation.
/// </summary>
public record AgentTask
{
    /// <summary>
    /// Unique identifier for the task.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Task name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Task description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Current status of the task.
    /// </summary>
    public TaskStatus Status { get; init; }

    /// <summary>
    /// When the task was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the task started.
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// When the task completed.
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int Progress { get; init; }

    /// <summary>
    /// Agent or service executing the task.
    /// </summary>
    public string? ExecutedBy { get; init; }

    /// <summary>
    /// Error message if the task failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Task metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Dependencies on other task IDs.
    /// </summary>
    public List<string> DependsOn { get; init; } = new();

    /// <summary>
    /// Whether the task is blocked by dependencies.
    /// </summary>
    public bool IsBlocked { get; init; }

    /// <summary>
    /// IDs of tasks that depend on this task.
    /// </summary>
    public List<string> Blocking { get; init; } = new();
}

/// <summary>
/// Status of a task.
/// </summary>
public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    Paused
}

/// <summary>
/// Interface for manager surface service.
/// Provides observation and orchestration of asynchronous agents.
/// </summary>
public interface IManagerSurfaceService
{
    /// <summary>
    /// Gets all active tasks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active tasks.</returns>
    Task<IReadOnlyList<AgentTask>> GetActiveTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task by ID.
    /// </summary>
    /// <param name="taskId">Task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task, or null if not found.</returns>
    Task<AgentTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="name">Task name.</param>
    /// <param name="description">Task description.</param>
    /// <param name="executedBy">Agent or service executing the task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created task.</returns>
    Task<AgentTask> CreateTaskAsync(
        string name,
        string description,
        string? executedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a task.
    /// </summary>
    /// <param name="taskId">Task ID.</param>
    /// <param name="status">New status.</param>
    /// <param name="progress">Progress percentage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    Task<AgentTask> UpdateTaskStatusAsync(
        string taskId,
        TaskStatus status,
        int progress = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a task.
    /// </summary>
    /// <param name="taskId">Task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    Task<AgentTask> CancelTaskAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a task.
    /// </summary>
    /// <param name="taskId">Task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    Task<AgentTask> PauseTaskAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused task.
    /// </summary>
    /// <param name="taskId">Task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    Task<AgentTask> ResumeTaskAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets task statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task statistics.</returns>
    Task<TaskStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a dependency between tasks.
    /// </summary>
    /// <param name="taskId">ID of the task that depends on another task.</param>
    /// <param name="dependsOnTaskId">ID of the task that is depended on.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated task.</returns>
    Task<AgentTask> AddDependencyAsync(
        string taskId,
        string dependsOnTaskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a task can be started (all dependencies are completed).
    /// </summary>
    /// <param name="taskId">Task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the task can be started, false otherwise.</returns>
    Task<bool> CanStartTaskAsync(
        string taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets blocked tasks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of blocked tasks.</returns>
    Task<IReadOnlyList<AgentTask>> GetBlockedTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblocks tasks that are waiting for a completed task.
    /// </summary>
    /// <param name="completedTaskId">ID of the completed task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of unblocked tasks.</returns>
    Task<IReadOnlyList<AgentTask>> UnblockDependentTasksAsync(
        string completedTaskId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Task statistics.
/// </summary>
public record TaskStatistics
{
    /// <summary>
    /// Total number of tasks.
    /// </summary>
    public int TotalTasks { get; init; }

    /// <summary>
    /// Number of running tasks.
    /// </summary>
    public int RunningTasks { get; init; }

    /// <summary>
    /// Number of completed tasks.
    /// </summary>
    public int CompletedTasks { get; init; }

    /// <summary>
    /// Number of failed tasks.
    /// </summary>
    public int FailedTasks { get; init; }

    /// <summary>
    /// Number of pending tasks.
    /// </summary>
    public int PendingTasks { get; init; }

    /// <summary>
    /// Average task duration in seconds.
    /// </summary>
    public double AverageDurationSeconds { get; init; }
}

/// <summary>
/// In-memory implementation of manager surface service.
/// </summary>
public class InMemoryManagerSurfaceService : IManagerSurfaceService
{
    private readonly Dictionary<string, AgentTask> _tasks = new();

    public Task<IReadOnlyList<AgentTask>> GetActiveTasksAsync(CancellationToken cancellationToken = default)
    {
        var activeTasks = _tasks.Values
            .Where(t => t.Status == TaskStatus.Running || t.Status == TaskStatus.Pending)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<AgentTask>>(activeTasks);
    }

    public Task<AgentTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        _tasks.TryGetValue(taskId, out var task);
        return Task.FromResult(task);
    }

    public Task<AgentTask> CreateTaskAsync(
        string name,
        string description,
        string? executedBy = null,
        CancellationToken cancellationToken = default)
    {
        var task = new AgentTask
        {
            Name = name,
            Description = description,
            Status = TaskStatus.Pending,
            ExecutedBy = executedBy
        };

        _tasks[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task<AgentTask> UpdateTaskStatusAsync(
        string taskId,
        TaskStatus status,
        int progress = 0,
        CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            throw new ArgumentException($"Task with ID {taskId} not found", nameof(taskId));
        }

        var now = DateTime.UtcNow;
        var updated = task with
        {
            Status = status,
            Progress = progress,
            StartedAt = status == TaskStatus.Running && !task.StartedAt.HasValue ? now : task.StartedAt,
            CompletedAt = (status == TaskStatus.Completed || status == TaskStatus.Failed) ? now : task.CompletedAt
        };

        _tasks[taskId] = updated;
        return Task.FromResult(updated);
    }

    public Task<AgentTask> CancelTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            throw new ArgumentException($"Task with ID {taskId} not found", nameof(taskId));
        }

        var updated = task with { Status = TaskStatus.Cancelled, CompletedAt = DateTime.UtcNow };
        _tasks[taskId] = updated;
        return Task.FromResult(updated);
    }

    public Task<AgentTask> PauseTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            throw new ArgumentException($"Task with ID {taskId} not found", nameof(taskId));
        }

        var updated = task with { Status = TaskStatus.Paused };
        _tasks[taskId] = updated;
        return Task.FromResult(updated);
    }

    public Task<AgentTask> ResumeTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            throw new ArgumentException($"Task with ID {taskId} not found", nameof(taskId));
        }

        var updated = task with { Status = TaskStatus.Running };
        _tasks[taskId] = updated;
        return Task.FromResult(updated);
    }

    public Task<TaskStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _tasks.Values.ToList();
        var completedTasks = tasks.Where(t => t.Status == TaskStatus.Completed).ToList();
        
        var avgDuration = completedTasks.Count > 0
            ? completedTasks.Average(t => 
                (t.CompletedAt - (t.StartedAt ?? t.CreatedAt))?.TotalSeconds ?? 0)
            : 0;

        var statistics = new TaskStatistics
        {
            TotalTasks = tasks.Count,
            RunningTasks = tasks.Count(t => t.Status == TaskStatus.Running),
            CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed),
            FailedTasks = tasks.Count(t => t.Status == TaskStatus.Failed),
            PendingTasks = tasks.Count(t => t.Status == TaskStatus.Pending),
            AverageDurationSeconds = avgDuration
        };

        return Task.FromResult(statistics);
    }

    public Task<AgentTask> AddDependencyAsync(
        string taskId,
        string dependsOnTaskId,
        CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            throw new ArgumentException($"Task with ID {taskId} not found", nameof(taskId));
        }

        if (!_tasks.TryGetValue(dependsOnTaskId, out var dependsOnTask))
        {
            throw new ArgumentException($"Task with ID {dependsOnTaskId} not found", nameof(dependsOnTaskId));
        }

        var updated = task with
        {
            DependsOn = task.DependsOn.Concat(new[] { dependsOnTaskId }).ToList()
        };

        _tasks[taskId] = updated;

        // Add blocking reference to the depended task
        var updatedDependsOn = dependsOnTask with
        {
            Blocking = dependsOnTask.Blocking.Concat(new[] { taskId }).ToList()
        };

        _tasks[dependsOnTaskId] = updatedDependsOn;

        return Task.FromResult(updated);
    }

    public Task<bool> CanStartTaskAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            return Task.FromResult(false);
        }

        foreach (var depId in task.DependsOn)
        {
            if (_tasks.TryGetValue(depId, out var depTask))
            {
                if (depTask.Status != TaskStatus.Completed)
                {
                    return Task.FromResult(false);
                }
            }
        }

        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<AgentTask>> GetBlockedTasksAsync(CancellationToken cancellationToken = default)
    {
        var blockedTasks = new List<AgentTask>();

        foreach (var task in _tasks.Values)
        {
            if (task.Status == TaskStatus.Pending && task.DependsOn.Any())
            {
                var canStart = task.DependsOn.All(depId =>
                {
                    if (_tasks.TryGetValue(depId, out var depTask))
                    {
                        return depTask.Status == TaskStatus.Completed;
                    }
                    return false;
                });

                if (!canStart)
                {
                    var blocked = task with { IsBlocked = true };
                    _tasks[task.Id] = blocked;
                    blockedTasks.Add(blocked);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<AgentTask>>(blockedTasks.AsReadOnly());
    }

    public async Task<IReadOnlyList<AgentTask>> UnblockDependentTasksAsync(
        string completedTaskId,
        CancellationToken cancellationToken = default)
    {
        var unblockedTasks = new List<AgentTask>();

        if (!_tasks.TryGetValue(completedTaskId, out var completedTask))
        {
            return unblockedTasks.AsReadOnly();
        }

        foreach (var blockingTaskId in completedTask.Blocking)
        {
            if (_tasks.TryGetValue(blockingTaskId, out var task))
            {
                var canStart = await CanStartTaskAsync(blockingTaskId, cancellationToken);
                if (canStart)
                {
                    var unblocked = task with { IsBlocked = false };
                    _tasks[blockingTaskId] = unblocked;
                    unblockedTasks.Add(unblocked);
                }
            }
        }

        return unblockedTasks.AsReadOnly();
    }
}
