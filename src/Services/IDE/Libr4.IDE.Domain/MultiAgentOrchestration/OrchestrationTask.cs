namespace Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Entity for orchestration tasks
/// </summary>
public class OrchestrationTask
{
    public Guid Id { get; private set; }
    public string TaskId { get; private set; }
    public string Description { get; private set; }
    public List<OrchestrationTask> Subtasks { get; private set; }
    public List<string> Dependencies { get; private set; }
    public Guid? AssignedAgentId { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private OrchestrationTask() { }
    
    public OrchestrationTask(
        string taskId,
        string description,
        List<string>? dependencies = null)
    {
        Id = Guid.NewGuid();
        TaskId = taskId;
        Description = description;
        Subtasks = new List<OrchestrationTask>();
        Dependencies = dependencies ?? new List<string>();
        AssignedAgentId = null;
        Status = "pending";
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddSubtask(OrchestrationTask subtask)
    {
        if (subtask != null)
        {
            Subtasks.Add(subtask);
        }
    }
    
    public void AddDependency(string taskId)
    {
        if (!string.IsNullOrWhiteSpace(taskId) && !Dependencies.Contains(taskId))
        {
            Dependencies.Add(taskId);
        }
    }
    
    public void AssignAgent(Guid agentId)
    {
        AssignedAgentId = agentId;
    }
    
    public void SetStatus(string status)
    {
        Status = status;
    }
    
    public static OrchestrationTask Create(
        string taskId,
        string description,
        List<string>? dependencies = null)
    {
        return new OrchestrationTask(taskId, description, dependencies);
    }
}
