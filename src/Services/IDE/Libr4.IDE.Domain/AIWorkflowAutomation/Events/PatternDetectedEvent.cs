using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.AIWorkflowAutomation.Events;

/// <summary>
/// Domain event raised when a pattern is detected
/// </summary>
public class PatternDetectedEvent : IDomainEvent
{
    public Guid WorkflowAnalysisId { get; }
    public string AnalysisId { get; }
    public string PatternName { get; }
    public DateTime OccurredOn { get; }
    
    public PatternDetectedEvent(
        Guid workflowAnalysisId,
        string analysisId,
        string patternName)
    {
        WorkflowAnalysisId = workflowAnalysisId;
        AnalysisId = analysisId;
        PatternName = patternName;
        OccurredOn = DateTime.UtcNow;
    }
}
