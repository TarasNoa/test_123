namespace Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// A phase with quality gates for controlled progression
/// </summary>
public class GatedPhase
{
    public string PhaseId { get; set; } = string.Empty;
    public string PhaseName { get; set; } = string.Empty;
    public string PhaseDescription { get; set; } = string.Empty;
    public List<QualityGate> Gates { get; set; } = new();
    public PhaseStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Order { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public string? FailureReason { get; set; }
    public TimeSpan Duration => CompletedAt.HasValue && StartedAt.HasValue 
        ? CompletedAt.Value - StartedAt.Value 
        : TimeSpan.Zero;
    
    public GatedPhase()
    {
    }
    
    public GatedPhase(string phaseId, string phaseName, string phaseDescription, int order = 0)
    {
        PhaseId = phaseId;
        PhaseName = phaseName;
        PhaseDescription = phaseDescription;
        Status = PhaseStatus.NotStarted;
        Order = order;
    }
    
    /// <summary>
    /// Check if all gates are passed
    /// </summary>
    public bool AllGatesPassed => Gates.All(g => g.IsPassedOrSkipped);
    
    /// <summary>
    /// Check if any gate failed
    /// </summary>
    public bool AnyGateFailed => Gates.Any(g => g.Status == GateStatus.Failed);
    
    /// <summary>
    /// Check if phase can proceed
    /// </summary>
    public bool CanProceed => Status == PhaseStatus.InProgress && AllGatesPassed;
    
    /// <summary>
    /// Check if phase is blocked by a gate
    /// </summary>
    public bool IsBlockedByGate => Gates.Any(g => g.IsBlockingProgression);
    
    /// <summary>
    /// Check if dependencies are satisfied
    /// </summary>
    public bool DependenciesSatisfied(List<string> completedPhases)
    {
        return Dependencies.All(dep => completedPhases.Contains(dep));
    }
    
    /// <summary>
    /// Mark phase as started
    /// </summary>
    public void MarkAsStarted()
    {
        Status = PhaseStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Mark phase as waiting for gate evaluation
    /// </summary>
    public void MarkAsWaitingForGate()
    {
        Status = PhaseStatus.WaitingForGate;
    }
    
    /// <summary>
    /// Mark phase as completed
    /// </summary>
    public void MarkAsCompleted()
    {
        Status = PhaseStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Mark phase as failed
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        Status = PhaseStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Mark phase as skipped
    /// </summary>
    public void MarkAsSkipped(string reason)
    {
        Status = PhaseStatus.Skipped;
        FailureReason = $"Skipped: {reason}";
        CompletedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Evaluate all gates
    /// </summary>
    public void EvaluateGates()
    {
        foreach (var gate in Gates.OrderBy(g => g.Order))
        {
            gate.Evaluate();
        }
        
        if (AllGatesPassed)
        {
            Status = PhaseStatus.InProgress;
        }
        else if (AnyGateFailed)
        {
            Status = PhaseStatus.WaitingForGate;
        }
    }
    
    /// <summary>
    /// Add a quality gate to the phase
    /// </summary>
    public void AddGate(QualityGate gate)
    {
        Gates.Add(gate);
    }
    
    /// <summary>
    /// Add a dependency phase
    /// </summary>
    public void AddDependency(string phaseId)
    {
        if (!Dependencies.Contains(phaseId))
        {
            Dependencies.Add(phaseId);
        }
    }
}
