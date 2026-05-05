namespace Libr4.IDE.Domain.TaskManagement;

/// <summary>
/// Background task status (from OpenHarness)
/// </summary>
public enum BackgroundTaskStatus
{
    Pending,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Background task lifecycle (from OpenHarness task management)
/// </summary>
public class BackgroundTask
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Dictionary<string, object> Parameters { get; private set; }
    public BackgroundTaskStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Result { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public List<string> OutputLog { get; private set; }
    
    public BackgroundTask(string name, string description, Dictionary<string, object>? parameters = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Parameters = parameters ?? new Dictionary<string, object>();
        Status = BackgroundTaskStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        OutputLog = new List<string>();
        RetryCount = 0;
    }
    
    public void Start()
    {
        if (Status != BackgroundTaskStatus.Pending && Status != BackgroundTaskStatus.Failed)
            return;
        
        Status = BackgroundTaskStatus.Running;
        StartedAt = DateTime.UtcNow;
    }
    
    public void Complete(string result)
    {
        Status = BackgroundTaskStatus.Completed;
        Result = result;
        CompletedAt = DateTime.UtcNow;
    }
    
    public void Fail(string errorMessage)
    {
        Status = BackgroundTaskStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
    
    public void Pause()
    {
        if (Status == BackgroundTaskStatus.Running)
            Status = BackgroundTaskStatus.Paused;
    }
    
    public void Resume()
    {
        if (Status == BackgroundTaskStatus.Paused)
            Status = BackgroundTaskStatus.Running;
    }
    
    public void Cancel()
    {
        Status = BackgroundTaskStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }
    
    public void Retry()
    {
        if (Status != BackgroundTaskStatus.Failed)
            return;
        
        if (RetryCount >= 3)
            return;
        
        RetryCount++;
        Status = BackgroundTaskStatus.Pending;
        ErrorMessage = null;
    }
    
    public void LogOutput(string message)
    {
        OutputLog.Add($"[{DateTime.UtcNow:HH:mm:ss}] {message}");
    }
}
