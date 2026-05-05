namespace Libr4.IDE.Domain.AutonomousRuntimePolicy;

/// <summary>
/// Represents runtime evidence signals
/// </summary>
public enum RuntimeEvidenceSignal
{
    /// <summary>
    /// No special evidence required
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Approval required
    /// </summary>
    ApprovalRequired = 1,
    
    /// <summary>
    /// Audit trail required
    /// </summary>
    AuditTrailRequired = 2,
    
    /// <summary>
    /// Both approval and audit trail required
    /// </summary>
    BothRequired = 3
}
