namespace Libr4.IDE.Domain.AIWorkflowAutomation;

/// <summary>
/// Entity representing a workflow pattern
/// </summary>
public class WorkflowPattern
{
    public Guid Id { get; private set; }
    public string PatternName { get; private set; }
    public string Description { get; private set; }
    public List<string> Steps { get; private set; }
    public int Frequency { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private WorkflowPattern() { }
    
    public WorkflowPattern(
        string patternName,
        string description,
        List<string>? steps = null,
        Dictionary<string, object>? metadata = null)
    {
        Id = Guid.NewGuid();
        PatternName = patternName;
        Description = description;
        Steps = steps ?? new List<string>();
        Frequency = 1;
        Metadata = metadata ?? new Dictionary<string, object>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void IncrementFrequency()
    {
        Frequency++;
    }
    
    public void AddStep(string step)
    {
        if (!string.IsNullOrWhiteSpace(step) && !Steps.Contains(step))
        {
            Steps.Add(step);
        }
    }
    
    public static WorkflowPattern Create(
        string patternName,
        string description,
        List<string>? steps = null,
        Dictionary<string, object>? metadata = null)
    {
        return new WorkflowPattern(patternName, description, steps, metadata);
    }
}
