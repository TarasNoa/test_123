namespace Libr4.IDE.Domain.TaskRecord;

/// <summary>
/// Entity representing a task checkpoint
/// </summary>
public class TaskCheckpoint
{
    public Guid Id { get; private set; }
    public string CheckpointName { get; private set; }
    public string SerializedState { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private TaskCheckpoint() { }
    
    public TaskCheckpoint(
        string checkpointName,
        string serializedState)
    {
        Id = Guid.NewGuid();
        CheckpointName = checkpointName;
        SerializedState = serializedState;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void UpdateState(string serializedState)
    {
        SerializedState = serializedState;
    }
    
    public static TaskCheckpoint Create(
        string checkpointName,
        string serializedState)
    {
        return new TaskCheckpoint(checkpointName, serializedState);
    }
}
