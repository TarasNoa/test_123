using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.TaskRecord.Events;

namespace Libr4.IDE.Domain.TaskRecord;

/// <summary>
/// AggregateRoot for task record
/// </summary>
public class TaskRecord : AggregateRoot<Guid>
{
    public string RecordId { get; private set; }
    public string TaskId { get; private set; }
    public TaskState State { get; private set; }
    public List<TaskCheckpoint> Checkpoints { get; private set; }
    public string CurrentState { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    private TaskRecord() { }
    
    public TaskRecord(
        string recordId,
        string taskId,
        string initialState = "{}")
    {
        Id = Guid.NewGuid();
        RecordId = recordId;
        TaskId = taskId;
        State = TaskState.Pending;
        Checkpoints = new List<TaskCheckpoint>();
        CurrentState = initialState;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = null;
    }
    
    public void SetState(TaskState state)
    {
        State = state;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetCurrentState(string state)
    {
        CurrentState = state;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void AddCheckpoint(TaskCheckpoint checkpoint)
    {
        if (checkpoint != null)
        {
            Checkpoints.Add(checkpoint);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public TaskCheckpoint? GetLatestCheckpoint()
    {
        return Checkpoints.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
    }
    
    /// <summary>
    /// Marks the task as created and raises a domain event
    /// </summary>
    public void MarkAsCreated()
    {
        AddDomainEvent(new TaskCreatedEvent(Id, RecordId));
    }
    
    /// <summary>
    /// Marks a checkpoint as created and raises a domain event
    /// </summary>
    public void MarkCheckpointCreated(TaskCheckpoint checkpoint)
    {
        AddDomainEvent(new CheckpointCreatedEvent(Id, RecordId, checkpoint.CheckpointName));
    }
    
    /// <summary>
    /// Marks the task as resumed and raises a domain event
    /// </summary>
    public void MarkAsResumed()
    {
        AddDomainEvent(new TaskResumedEvent(Id, RecordId));
    }
    
    public static TaskRecord Create(
        string recordId,
        string taskId,
        string initialState = "{}")
    {
        return new TaskRecord(recordId, taskId, initialState);
    }
}
