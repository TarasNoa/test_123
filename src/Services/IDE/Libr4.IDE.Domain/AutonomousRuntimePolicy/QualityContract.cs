namespace Libr4.IDE.Domain.AutonomousRuntimePolicy;

/// <summary>
/// Value object for quality contract
/// </summary>
public class QualityContract
{
    public bool ApprovalRequired { get; private set; }
    public bool AuditTrailRequired { get; private set; }
    public List<string> QualityChecks { get; private set; }
    public Dictionary<string, object> QualityThresholds { get; private set; }
    public string ApprovalWorkflow { get; private set; }
    
    private QualityContract() { }
    
    public QualityContract(
        bool approvalRequired,
        bool auditTrailRequired,
        List<string>? qualityChecks,
        Dictionary<string, object>? qualityThresholds,
        string approvalWorkflow)
    {
        ApprovalRequired = approvalRequired;
        AuditTrailRequired = auditTrailRequired;
        QualityChecks = qualityChecks ?? new List<string>();
        QualityThresholds = qualityThresholds ?? new Dictionary<string, object>();
        ApprovalWorkflow = approvalWorkflow;
    }
    
    public void AddQualityCheck(string check)
    {
        if (!string.IsNullOrWhiteSpace(check) && !QualityChecks.Contains(check))
        {
            QualityChecks.Add(check);
        }
    }
    
    public void SetQualityThreshold(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            QualityThresholds[key] = value;
        }
    }
    
    public static QualityContract Create(
        bool approvalRequired,
        bool auditTrailRequired,
        List<string>? qualityChecks = null,
        Dictionary<string, object>? qualityThresholds = null,
        string approvalWorkflow = "standard_review")
    {
        return new QualityContract(approvalRequired, auditTrailRequired, qualityChecks, qualityThresholds, approvalWorkflow);
    }
}
