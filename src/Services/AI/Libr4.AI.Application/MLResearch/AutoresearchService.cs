namespace Libr4.AI.Application.MLResearch;

using Libr4.AI.Domain.MLResearch;

/// <summary>
/// Service for autoresearch orchestration
/// </summary>
public interface IAutoresearchService
{
    /// <summary>
    /// Create a new research task
    /// </summary>
    ResearchTask CreateResearchTask(string researchQuestion);
    
    /// <summary>
    /// Add a subtask to a research task
    /// </summary>
    void AddSubtask(ResearchTask task, ResearchSubtask subtask);
    
    /// <summary>
    /// Execute a research task
    /// </summary>
    Task<ResearchTask> ExecuteResearchTaskAsync(ResearchTask task, CancellationToken ct = default);
    
    /// <summary>
    /// Execute a single subtask
    /// </summary>
    Task<ResearchSubtask> ExecuteSubtaskAsync(ResearchTask task, string subtaskId, CancellationToken ct = default);
    
    /// <summary>
    /// Get research task by ID
    /// </summary>
    ResearchTask? GetResearchTask(string taskId);
    
    /// <summary>
    /// Create a verification plan for a research task
    /// </summary>
    MechanicalVerificationPlan CreateVerificationPlan(ResearchTask task);
    
    /// <summary>
    /// Execute verification plan
    /// </summary>
    Task<VerificationSummary> ExecuteVerificationAsync(ResearchTask task, CancellationToken ct = default);
    
    /// <summary>
    /// Auto-rollback research task if verification fails
    /// </summary>
    Task<bool> AutoRollbackIfFailedAsync(ResearchTask task, CancellationToken ct = default);
}

public class AutoresearchService : IAutoresearchService
{
    private readonly Dictionary<string, ResearchTask> _researchTasks = new();
    private readonly IMechanicalVerificationService _verificationService;
    private readonly IRollbackService _rollbackService;
    
    public AutoresearchService(
        IMechanicalVerificationService verificationService,
        IRollbackService rollbackService)
    {
        _verificationService = verificationService;
        _rollbackService = rollbackService;
    }
    
    public ResearchTask CreateResearchTask(string researchQuestion)
    {
        var taskId = Guid.NewGuid().ToString();
        var task = new ResearchTask(taskId, researchQuestion);
        _researchTasks[taskId] = task;
        return task;
    }
    
    public void AddSubtask(ResearchTask task, ResearchSubtask subtask)
    {
        task.AddSubtask(subtask);
    }
    
    public async Task<ResearchTask> ExecuteResearchTaskAsync(ResearchTask task, CancellationToken ct)
    {
        task.MarkAsStarted();
        
        try
        {
            var subtasks = task.GetOrderedSubtasks();
            foreach (var subtask in subtasks)
            {
                ct.ThrowIfCancellationRequested();
                
                var executableSubtask = task.GetNextExecutableSubtask();
                if (executableSubtask != null)
                {
                    await ExecuteSubtaskAsync(task, executableSubtask.SubtaskId, ct);
                }
            }
            
            task.MarkAsCompleted("Research completed successfully");
            return task;
        }
        catch (OperationCanceledException)
        {
            task.Status = ResearchStatus.Cancelled;
            task.CompletedAt = DateTime.UtcNow;
            return task;
        }
        catch (Exception ex)
        {
            task.MarkAsFailed(ex.Message);
            return task;
        }
    }
    
    public async Task<ResearchSubtask> ExecuteSubtaskAsync(ResearchTask task, string subtaskId, CancellationToken ct)
    {
        var subtask = task.Subtasks.FirstOrDefault(s => s.SubtaskId == subtaskId);
        if (subtask == null)
        {
            throw new ArgumentException($"Subtask {subtaskId} not found");
        }
        
        subtask.MarkAsStarted();
        
        try
        {
            // Execute subtask logic here
            // For now, simulate execution
            await Task.Delay(100, ct);
            
            subtask.MarkAsCompleted("Subtask completed (mock)");
            return subtask;
        }
        catch (Exception ex)
        {
            subtask.MarkAsFailed(ex.Message);
            return subtask;
        }
    }
    
    public ResearchTask? GetResearchTask(string taskId)
    {
        return _researchTasks.TryGetValue(taskId, out var task) ? task : null;
    }
    
    public MechanicalVerificationPlan CreateVerificationPlan(ResearchTask task)
    {
        var plan = _verificationService.CreatePlan(task.TaskId);
        task.VerificationPlan = plan;
        return plan;
    }
    
    public async Task<VerificationSummary> ExecuteVerificationAsync(ResearchTask task, CancellationToken ct)
    {
        if (task.VerificationPlan == null)
        {
            throw new InvalidOperationException("No verification plan for task");
        }
        
        return await _verificationService.ExecutePlanAsync(task.VerificationPlan, ct);
    }
    
    public async Task<bool> AutoRollbackIfFailedAsync(ResearchTask task, CancellationToken ct)
    {
        if (task.VerificationPlan == null || task.VerificationPlan.IsSuccessfullyCompleted)
        {
            return false;
        }
        
        var rollbackOperation = await _rollbackService.AutoRollbackAsync(
            task.TaskId,
            RollbackTrigger.VerificationFailed,
            ct);
        
        return rollbackOperation?.Succeeded ?? false;
    }
}
