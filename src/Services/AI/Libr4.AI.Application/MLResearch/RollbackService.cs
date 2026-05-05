namespace Libr4.AI.Application.MLResearch;

using Libr4.AI.Domain.MLResearch;

/// <summary>
/// Service for automatic rollback operations
/// </summary>
public interface IRollbackService
{
    /// <summary>
    /// Create a rollback checkpoint
    /// </summary>
    RollbackCheckpoint CreateCheckpoint(string researchTaskId, string stateSnapshot, string? description = null);
    
    /// <summary>
    /// Create a rollback operation
    /// </summary>
    RollbackOperation CreateRollbackOperation(RollbackTrigger trigger, RollbackCheckpoint targetCheckpoint);
    
    /// <summary>
    /// Execute rollback operation
    /// </summary>
    Task<RollbackOperation> ExecuteRollbackAsync(RollbackOperation operation, CancellationToken ct = default);
    
    /// <summary>
    /// Get latest checkpoint for a research task
    /// </summary>
    RollbackCheckpoint? GetLatestCheckpoint(string researchTaskId);
    
    /// <summary>
    /// Get all checkpoints for a research task
    /// </summary>
    List<RollbackCheckpoint> GetCheckpoints(string researchTaskId);
    
    /// <summary>
    /// Delete a checkpoint
    /// </summary>
    void DeleteCheckpoint(string checkpointId);
    
    /// <summary>
    /// Auto-rollback based on trigger
    /// </summary>
    Task<RollbackOperation?> AutoRollbackAsync(string researchTaskId, RollbackTrigger trigger, CancellationToken ct = default);
}

public class RollbackService : IRollbackService
{
    private readonly Dictionary<string, List<RollbackCheckpoint>> _taskCheckpoints = new();
    private readonly Dictionary<string, RollbackOperation> _operations = new();
    
    public RollbackCheckpoint CreateCheckpoint(string researchTaskId, string stateSnapshot, string? description = null)
    {
        var checkpointId = Guid.NewGuid().ToString();
        var checkpoint = new RollbackCheckpoint(checkpointId, researchTaskId, stateSnapshot)
        {
            Description = description,
            IsAutomatic = true
        };
        
        if (!_taskCheckpoints.ContainsKey(researchTaskId))
        {
            _taskCheckpoints[researchTaskId] = new List<RollbackCheckpoint>();
        }
        
        _taskCheckpoints[researchTaskId].Add(checkpoint);
        return checkpoint;
    }
    
    public RollbackOperation CreateRollbackOperation(RollbackTrigger trigger, RollbackCheckpoint targetCheckpoint)
    {
        var operationId = Guid.NewGuid().ToString();
        var operation = new RollbackOperation(operationId, trigger, targetCheckpoint);
        _operations[operationId] = operation;
        return operation;
    }
    
    public async Task<RollbackOperation> ExecuteRollbackAsync(RollbackOperation operation, CancellationToken ct)
    {
        operation.MarkAsStarted();
        
        try
        {
            // Restore modified files
            foreach (var file in operation.TargetCheckpoint.ModifiedFiles)
            {
                ct.ThrowIfCancellationRequested();
                
                // In a real implementation, restore file from checkpoint
                operation.AddRestoredFile(file);
                await Task.Delay(10, ct); // Simulate file restoration
            }
            
            // Delete created files
            foreach (var file in operation.TargetCheckpoint.CreatedFiles)
            {
                ct.ThrowIfCancellationRequested();
                
                // In a real implementation, delete the file
                operation.AddDeletedFile(file);
                await Task.Delay(10, ct); // Simulate file deletion
            }
            
            // Restore deleted files
            foreach (var file in operation.TargetCheckpoint.DeletedFiles)
            {
                ct.ThrowIfCancellationRequested();
                
                // In a real implementation, restore the file
                operation.AddRestoredFile(file);
                await Task.Delay(10, ct); // Simulate file restoration
            }
            
            operation.MarkAsCompleted();
            return operation;
        }
        catch (OperationCanceledException)
        {
            operation.MarkAsCancelled();
            return operation;
        }
        catch (Exception ex)
        {
            operation.MarkAsFailed(ex.Message);
            return operation;
        }
    }
    
    public RollbackCheckpoint? GetLatestCheckpoint(string researchTaskId)
    {
        if (_taskCheckpoints.TryGetValue(researchTaskId, out var checkpoints))
        {
            return checkpoints.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
        }
        return null;
    }
    
    public List<RollbackCheckpoint> GetCheckpoints(string researchTaskId)
    {
        if (_taskCheckpoints.TryGetValue(researchTaskId, out var checkpoints))
        {
            return checkpoints.OrderByDescending(c => c.CreatedAt).ToList();
        }
        return new List<RollbackCheckpoint>();
    }
    
    public void DeleteCheckpoint(string checkpointId)
    {
        foreach (var taskCheckpoints in _taskCheckpoints.Values)
        {
            var checkpoint = taskCheckpoints.FirstOrDefault(c => c.CheckpointId == checkpointId);
            if (checkpoint != null)
            {
                taskCheckpoints.Remove(checkpoint);
                break;
            }
        }
    }
    
    public async Task<RollbackOperation?> AutoRollbackAsync(string researchTaskId, RollbackTrigger trigger, CancellationToken ct)
    {
        var latestCheckpoint = GetLatestCheckpoint(researchTaskId);
        if (latestCheckpoint == null)
        {
            return null;
        }
        
        var operation = CreateRollbackOperation(trigger, latestCheckpoint);
        return await ExecuteRollbackAsync(operation, ct);
    }
}
