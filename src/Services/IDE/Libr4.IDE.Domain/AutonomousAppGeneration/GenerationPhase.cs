namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Thoroughness level for gated phases (from agentsys)
/// </summary>
public enum PhaseThoroughness
{
    /// <summary>
    /// Quick - basic checks only
    /// </summary>
    Quick,
    
    /// <summary>
    /// Normal - balanced approach
    /// </summary>
    Normal,
    
    /// <summary>
    /// Deep - comprehensive analysis
    /// </summary>
    Deep
}

/// <summary>
/// A single phase inside the overall generation plan
/// (e.g. "Scaffold project", "Implement domain", "Write tests", "Run integration tests").
/// Phases are gated - they only run if conditions are met.
/// </summary>
public sealed class GenerationPhase
{
    public int Order { get; }
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<AgentAssignment> Assignments { get; }
    
    /// <summary>
    /// Certainty level for findings from this phase
    /// </summary>
    public CertaintyLevel FindingsCertainty { get; }
    
    /// <summary>
    /// Thoroughness level for this phase
    /// </summary>
    public PhaseThoroughness Thoroughness { get; }
    
    /// <summary>
    /// Conditions that must be met for this phase to run
    /// </summary>
    public IReadOnlyList<string> RequiredConditions { get; }
    
    /// <summary>
    /// Phases that must complete before this phase can run
    /// </summary>
    public IReadOnlyList<int> Dependencies { get; }
    
    /// <summary>
    /// Whether this phase is currently blocked
    /// </summary>
    public bool IsBlocked { get; private set; }
    
    /// <summary>
    /// Reason why this phase is blocked
    /// </summary>
    public string? BlockReason { get; private set; }

    public GenerationPhase(
        int order,
        string name,
        string description,
        IReadOnlyList<AgentAssignment> assignments,
        CertaintyLevel findingsCertainty = CertaintyLevel.Medium,
        PhaseThoroughness thoroughness = PhaseThoroughness.Normal,
        IReadOnlyList<string>? requiredConditions = null,
        IReadOnlyList<int>? dependencies = null)
    {
        Order = order;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        Assignments = assignments ?? new List<AgentAssignment>();
        FindingsCertainty = findingsCertainty;
        Thoroughness = thoroughness;
        RequiredConditions = requiredConditions ?? new List<string>();
        Dependencies = dependencies ?? new List<int>();
        IsBlocked = false;
    }
    
    /// <summary>
    /// Check if phase can run based on conditions
    /// </summary>
    public bool CanRun(Dictionary<string, bool> conditionStates)
    {
        if (IsBlocked) return false;
        
        foreach (var condition in RequiredConditions)
        {
            if (!conditionStates.TryGetValue(condition, out var met) || !met)
            {
                BlockReason = $"Condition not met: {condition}";
                IsBlocked = true;
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Block this phase with a reason
    /// </summary>
    public void Block(string reason)
    {
        IsBlocked = true;
        BlockReason = reason;
    }
    
    /// <summary>
    /// Unblock this phase
    /// </summary>
    public void Unblock()
    {
        IsBlocked = false;
        BlockReason = null;
    }
}
