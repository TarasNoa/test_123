namespace Libr4.IDE.Domain.OrchestrationRun;

/// <summary>
/// Value object representing a workflow transition
/// </summary>
public class WorkflowTransition
{
    public string FromState { get; private set; }
    public string ToState { get; private set; }
    public string TransitionType { get; private set; }
    public Dictionary<string, object> TransitionData { get; private set; }
    public DateTime TransitionedAt { get; private set; }
    
    private WorkflowTransition() { }
    
    public WorkflowTransition(
        string fromState,
        string toState,
        string transitionType,
        Dictionary<string, object>? transitionData,
        DateTime? transitionedAt = null)
    {
        FromState = fromState;
        ToState = toState;
        TransitionType = transitionType;
        TransitionData = transitionData ?? new Dictionary<string, object>();
        TransitionedAt = transitionedAt ?? DateTime.UtcNow;
    }
    
    public void AddTransitionData(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            TransitionData[key] = value;
        }
    }
    
    public static WorkflowTransition Create(
        string fromState,
        string toState,
        string transitionType,
        Dictionary<string, object>? transitionData = null,
        DateTime? transitionedAt = null)
    {
        return new WorkflowTransition(fromState, toState, transitionType, transitionData, transitionedAt);
    }
}
