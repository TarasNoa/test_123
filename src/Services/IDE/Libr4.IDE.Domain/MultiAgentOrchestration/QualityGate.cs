namespace Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Status of a quality gate
/// </summary>
public enum GateStatus
{
    Pending,
    Passed,
    Failed,
    Skipped
}

/// <summary>
/// A quality gate criterion that must be met to proceed
/// </summary>
public class GateCriterion
{
    public string CriterionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMet { get; set; }
    public string? Evidence { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    
    public GateCriterion()
    {
    }
    
    public GateCriterion(string criterionId, string description)
    {
        CriterionId = criterionId;
        Description = description;
        IsMet = false;
    }
    
    public void MarkAsPassed(string? evidence = null)
    {
        IsMet = true;
        Evidence = evidence;
        FailureReason = null;
        EvaluatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsFailed(string failureReason)
    {
        IsMet = false;
        FailureReason = failureReason;
        EvaluatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsSkipped(string? reason = null)
    {
        IsMet = true; // Skipped gates are considered passed
        Evidence = $"Skipped: {reason}";
        EvaluatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// A quality gate that controls progression between phases
/// </summary>
public class QualityGate
{
    public string GateId { get; set; } = string.Empty;
    public string GateName { get; set; } = string.Empty;
    public string GateDescription { get; set; } = string.Empty;
    public GateStatus Status { get; set; }
    public List<GateCriterion> Criteria { get; set; } = new();
    public DateTime? PassedAt { get; set; }
    public string? FailureReason { get; set; }
    public bool IsBlocking { get; set; } = true;
    public int Order { get; set; }
    
    public QualityGate()
    {
    }
    
    public QualityGate(string gateId, string gateName, string gateDescription, int order = 0)
    {
        GateId = gateId;
        GateName = gateName;
        GateDescription = gateDescription;
        Status = GateStatus.Pending;
        Order = order;
    }
    
    /// <summary>
    /// Evaluate the gate based on criteria
    /// </summary>
    public void Evaluate()
    {
        if (Criteria.Count == 0)
        {
            Status = GateStatus.Passed;
            PassedAt = DateTime.UtcNow;
            return;
        }
        
        var allMet = Criteria.All(c => c.IsMet);
        var anyFailed = Criteria.Any(c => !c.IsMet && c.EvaluatedAt.HasValue);
        
        if (allMet)
        {
            Status = GateStatus.Passed;
            PassedAt = DateTime.UtcNow;
            FailureReason = null;
        }
        else if (anyFailed)
        {
            Status = GateStatus.Failed;
            var failedCriteria = Criteria.Where(c => !c.IsMet).ToList();
            FailureReason = string.Join("; ", failedCriteria.Select(c => c.FailureReason));
        }
        // else remain Pending
    }
    
    /// <summary>
    /// Check if gate is passed or skipped
    /// </summary>
    public bool IsPassedOrSkipped => Status == GateStatus.Passed || Status == GateStatus.Skipped;
    
    /// <summary>
    /// Check if gate is blocking progression
    /// </summary>
    public bool IsBlockingProgression => IsBlocking && !IsPassedOrSkipped;
}
