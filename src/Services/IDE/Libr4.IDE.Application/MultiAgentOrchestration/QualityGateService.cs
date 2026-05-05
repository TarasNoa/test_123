namespace Libr4.IDE.Application.MultiAgentOrchestration;

using Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Service for managing quality gates in multi-agent orchestration
/// </summary>
public interface IQualityGateService
{
    /// <summary>
    /// Create a new quality gate
    /// </summary>
    QualityGate CreateGate(string gateId, string gateName, string gateDescription, int order = 0);
    
    /// <summary>
    /// Add a criterion to a quality gate
    /// </summary>
    void AddCriterion(QualityGate gate, string criterionId, string description);
    
    /// <summary>
    /// Evaluate a quality gate
    /// </summary>
    void EvaluateGate(QualityGate gate);
    
    /// <summary>
    /// Mark a criterion as passed
    /// </summary>
    void MarkCriterionAsPassed(QualityGate gate, string criterionId, string? evidence = null);
    
    /// <summary>
    /// Mark a criterion as failed
    /// </summary>
    void MarkCriterionAsFailed(QualityGate gate, string criterionId, string failureReason);
    
    /// <summary>
    /// Skip a criterion
    /// </summary>
    void SkipCriterion(QualityGate gate, string criterionId, string? reason = null);
    
    /// <summary>
    /// Create a new gated phase
    /// </summary>
    GatedPhase CreatePhase(string phaseId, string phaseName, string phaseDescription, int order = 0);
    
    /// <summary>
    /// Add a gate to a phase
    /// </summary>
    void AddGateToPhase(GatedPhase phase, QualityGate gate);
    
    /// <summary>
    /// Start a phase
    /// </summary>
    void StartPhase(GatedPhase phase);
    
    /// <summary>
    /// Evaluate all gates in a phase
    /// </summary>
    void EvaluatePhaseGates(GatedPhase phase);
    
    /// <summary>
    /// Complete a phase
    /// </summary>
    void CompletePhase(GatedPhase phase);
    
    /// <summary>
    /// Fail a phase
    /// </summary>
    void FailPhase(GatedPhase phase, string reason);
    
    /// <summary>
    /// Skip a phase
    /// </summary>
    void SkipPhase(GatedPhase phase, string reason);
    
    /// <summary>
    /// Add a dependency to a phase
    /// </summary>
    void AddPhaseDependency(GatedPhase phase, string dependencyPhaseId);
    
    /// <summary>
    /// Check if phase can start based on dependencies
    /// </summary>
    bool CanPhaseStart(GatedPhase phase, List<string> completedPhases);
    
    /// <summary>
    /// Get summary of phase status
    /// </summary>
    PhaseStatusSummary GetPhaseStatusSummary(GatedPhase phase);
}

/// <summary>
/// Summary of phase status
/// </summary>
public class PhaseStatusSummary
{
    public string PhaseId { get; set; } = string.Empty;
    public string PhaseName { get; set; } = string.Empty;
    public PhaseStatus Status { get; set; }
    public int TotalGates { get; set; }
    public int PassedGates { get; set; }
    public int FailedGates { get; set; }
    public int SkippedGates { get; set; }
    public int PendingGates { get; set; }
    public bool IsBlocked { get; set; }
    public TimeSpan Duration { get; set; }
    public string? FailureReason { get; set; }
}

public class QualityGateService : IQualityGateService
{
    public QualityGate CreateGate(string gateId, string gateName, string gateDescription, int order = 0)
    {
        return new QualityGate(gateId, gateName, gateDescription, order);
    }
    
    public void AddCriterion(QualityGate gate, string criterionId, string description)
    {
        var criterion = new GateCriterion(criterionId, description);
        gate.Criteria.Add(criterion);
    }
    
    public void EvaluateGate(QualityGate gate)
    {
        gate.Evaluate();
    }
    
    public void MarkCriterionAsPassed(QualityGate gate, string criterionId, string? evidence = null)
    {
        var criterion = gate.Criteria.FirstOrDefault(c => c.CriterionId == criterionId);
        if (criterion != null)
        {
            criterion.MarkAsPassed(evidence);
            gate.Evaluate();
        }
    }
    
    public void MarkCriterionAsFailed(QualityGate gate, string criterionId, string failureReason)
    {
        var criterion = gate.Criteria.FirstOrDefault(c => c.CriterionId == criterionId);
        if (criterion != null)
        {
            criterion.MarkAsFailed(failureReason);
            gate.Evaluate();
        }
    }
    
    public void SkipCriterion(QualityGate gate, string criterionId, string? reason = null)
    {
        var criterion = gate.Criteria.FirstOrDefault(c => c.CriterionId == criterionId);
        if (criterion != null)
        {
            criterion.MarkAsSkipped(reason);
            gate.Evaluate();
        }
    }
    
    public GatedPhase CreatePhase(string phaseId, string phaseName, string phaseDescription, int order = 0)
    {
        return new GatedPhase(phaseId, phaseName, phaseDescription, order);
    }
    
    public void AddGateToPhase(GatedPhase phase, QualityGate gate)
    {
        phase.AddGate(gate);
    }
    
    public void StartPhase(GatedPhase phase)
    {
        if (phase.Status != PhaseStatus.NotStarted)
        {
            throw new InvalidOperationException($"Phase {phase.PhaseId} is not in NotStarted status");
        }
        
        phase.MarkAsStarted();
    }
    
    public void EvaluatePhaseGates(GatedPhase phase)
    {
        phase.EvaluateGates();
    }
    
    public void CompletePhase(GatedPhase phase)
    {
        if (phase.Status != PhaseStatus.InProgress && phase.Status != PhaseStatus.WaitingForGate)
        {
            throw new InvalidOperationException($"Phase {phase.PhaseId} is not in InProgress or WaitingForGate status");
        }
        
        if (!phase.AllGatesPassed)
        {
            throw new InvalidOperationException($"Phase {phase.PhaseId} cannot be completed: not all gates passed");
        }
        
        phase.MarkAsCompleted();
    }
    
    public void FailPhase(GatedPhase phase, string reason)
    {
        phase.MarkAsFailed(reason);
    }
    
    public void SkipPhase(GatedPhase phase, string reason)
    {
        phase.MarkAsSkipped(reason);
    }
    
    public void AddPhaseDependency(GatedPhase phase, string dependencyPhaseId)
    {
        phase.AddDependency(dependencyPhaseId);
    }
    
    public bool CanPhaseStart(GatedPhase phase, List<string> completedPhases)
    {
        return phase.DependenciesSatisfied(completedPhases);
    }
    
    public PhaseStatusSummary GetPhaseStatusSummary(GatedPhase phase)
    {
        return new PhaseStatusSummary
        {
            PhaseId = phase.PhaseId,
            PhaseName = phase.PhaseName,
            Status = phase.Status,
            TotalGates = phase.Gates.Count,
            PassedGates = phase.Gates.Count(g => g.Status == GateStatus.Passed),
            FailedGates = phase.Gates.Count(g => g.Status == GateStatus.Failed),
            SkippedGates = phase.Gates.Count(g => g.Status == GateStatus.Skipped),
            PendingGates = phase.Gates.Count(g => g.Status == GateStatus.Pending),
            IsBlocked = phase.IsBlockedByGate,
            Duration = phase.Duration,
            FailureReason = phase.FailureReason
        };
    }
}
