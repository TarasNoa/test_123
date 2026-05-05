namespace Libr4.IDE.Domain.TaskRecord;

/// <summary>
/// Represents the state of a task
/// </summary>
public enum TaskState
{
    /// <summary>
    /// Task is waiting to start
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Task is currently running
    /// </summary>
    Running = 1,
    
    /// <summary>
    /// Task is paused
    /// </summary>
    Paused = 2,
    
    /// <summary>
    /// Task completed successfully
    /// </summary>
    Completed = 3,
    
    /// <summary>
    /// Task failed
    /// </summary>
    Failed = 4
}
