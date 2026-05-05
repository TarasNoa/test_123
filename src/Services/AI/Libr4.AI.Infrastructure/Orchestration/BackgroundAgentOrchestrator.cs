using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Orchestration;

/// <summary>
/// Implementation of background agent orchestrator
/// Uses in-memory execution for now - can be extended to use message queues
/// </summary>
public class BackgroundAgentOrchestrator : IBackgroundAgentOrchestrator
{
    private readonly ILogger<BackgroundAgentOrchestrator> _logger;
    private readonly Dictionary<Guid, AgentTaskStatus> _activeTasks = new();
    private readonly Dictionary<Guid, AgentTask> _taskDefinitions = new();
    private readonly SemaphoreSlim _executionSemaphore;

    public BackgroundAgentOrchestrator(ILogger<BackgroundAgentOrchestrator> logger)
    {
        _logger = logger;
        _executionSemaphore = new SemaphoreSlim(5); // Max 5 concurrent agents
    }

    public async Task<Guid> DispatchAsync(AgentTask task)
    {
        task.Id = Guid.NewGuid();
        task.CreatedAt = DateTimeOffset.UtcNow;
        
        var status = new AgentTaskStatus
        {
            TaskId = task.Id,
            State = TaskState.Queued,
            CurrentIteration = 0,
            StartedAt = DateTimeOffset.UtcNow
        };
        
        _taskDefinitions[task.Id] = task;
        _activeTasks[task.Id] = status;
        
        // Start execution in background
        _ = Task.Run(() => ExecuteTaskAsync(task, status));
        
        _logger.LogInformation("Dispatched background task: {TaskId}", task.Id);
        return task.Id;
    }

    public async Task<AgentTaskStatus?> GetTaskStatusAsync(Guid taskId)
    {
        _activeTasks.TryGetValue(taskId, out var status);
        return status;
    }

    public async Task<AgentTaskResult?> WaitForCompletionAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var status = await GetTaskStatusAsync(taskId);
            if (status == null)
                return null;
            
            if (status.State == TaskState.Completed || status.State == TaskState.Failed || status.State == TaskState.Cancelled)
            {
                return new AgentTaskResult
                {
                    TaskId = taskId,
                    Success = status.State == TaskState.Completed,
                    Output = status.Output,
                    Error = status.Error,
                    IterationsCompleted = status.CurrentIteration,
                    Duration = (status.CompletedAt ?? DateTimeOffset.UtcNow) - status.StartedAt
                };
            }
            
            await Task.Delay(500, cancellationToken);
        }
        
        return null;
    }

    public async Task CancelTaskAsync(Guid taskId)
    {
        if (_activeTasks.TryGetValue(taskId, out var status))
        {
            status.State = TaskState.Cancelled;
            status.CompletedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation("Cancelled task: {TaskId}", taskId);
        }
    }

    public async Task<List<AgentTaskStatus>> ListActiveTasksAsync()
    {
        return _activeTasks.Values
            .Where(s => s.State == TaskState.Running || s.State == TaskState.Queued)
            .ToList();
    }

    public async Task<List<AgentTaskResult>> RunParallelAgentsAsync(
        AgentTask task,
        int agentCount,
        CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task<AgentTaskResult>>();
        
        for (int i = 0; i < agentCount; i++)
        {
            var agentTask = new AgentTask
            {
                Description = $"{task.Description} (Agent {i + 1})",
                Context = task.Context,
                Parameters = new Dictionary<string, object>(task.Parameters),
                ModelId = task.ModelId,
                MaxIterations = task.MaxIterations,
                TimeoutSeconds = task.TimeoutSeconds
            };
            
            var taskId = await DispatchAsync(agentTask);
            tasks.Add(WaitForCompletionAsync(taskId, cancellationToken)!);
        }
        
        var results = await Task.WhenAll(tasks);
        _logger.LogInformation("Parallel agents completed: {Count}/{Total} successful", 
            results.Count(r => r.Success), results.Length);
        
        return results.ToList();
    }

    private async Task ExecuteTaskAsync(AgentTask task, AgentTaskStatus status)
    {
        await _executionSemaphore.WaitAsync();
        
        try
        {
            status.State = TaskState.Running;
            
            for (int iteration = 0; iteration < task.MaxIterations; iteration++)
            {
                if (status.State == TaskState.Cancelled)
                    break;
                
                status.CurrentIteration = iteration + 1;
                status.CurrentStep = $"Iteration {iteration + 1}/{task.MaxIterations}";
                
                // Simulate agent execution
                // In production, this would call the actual AI service
                await Task.Delay(1000); // Simulate work
                
                // Simulate completion after 3 iterations
                if (iteration >= 2)
                {
                    status.State = TaskState.Completed;
                    status.Output = $"Task completed after {iteration + 1} iterations";
                    status.CompletedAt = DateTimeOffset.UtcNow;
                    _logger.LogInformation("Task completed: {TaskId} after {Iterations} iterations", 
                        task.Id, iteration + 1);
                    break;
                }
                
                // Check timeout
                if ((DateTimeOffset.UtcNow - status.StartedAt).TotalSeconds > task.TimeoutSeconds)
                {
                    status.State = TaskState.Failed;
                    status.Error = "Task timeout";
                    status.CompletedAt = DateTimeOffset.UtcNow;
                    _logger.LogWarning("Task timed out: {TaskId}", task.Id);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            status.State = TaskState.Failed;
            status.Error = ex.Message;
            status.CompletedAt = DateTimeOffset.UtcNow;
            _logger.LogError(ex, "Task failed: {TaskId}", task.Id);
        }
        finally
        {
            _executionSemaphore.Release();
            _activeTasks.Remove(task.Id);
            _taskDefinitions.Remove(task.Id);
        }
    }
}
