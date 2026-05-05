using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.AIWorkflowAutomation.Events;

/// <summary>
/// Domain event raised when workflow analysis is started
/// </summary>
public class AnalysisStartedEvent : IDomainEvent
{
    public Guid WorkflowAnalysisId { get; }
    public string AnalysisId { get; }
    public DateTime OccurredOn { get; }
    
    public AnalysisStartedEvent(
        Guid workflowAnalysisId,
        string analysisId)
    {
        WorkflowAnalysisId = workflowAnalysisId;
        AnalysisId = analysisId;
        OccurredOn = DateTime.UtcNow;
    }
}
