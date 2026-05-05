using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.AIWorkflowAutomation.Events;

/// <summary>
/// Domain event raised when a skill is extracted
/// </summary>
public class SkillExtractedEvent : IDomainEvent
{
    public Guid WorkflowAnalysisId { get; }
    public string AnalysisId { get; }
    public string SkillName { get; }
    public DateTime OccurredOn { get; }
    
    public SkillExtractedEvent(
        Guid workflowAnalysisId,
        string analysisId,
        string skillName)
    {
        WorkflowAnalysisId = workflowAnalysisId;
        AnalysisId = analysisId;
        SkillName = skillName;
        OccurredOn = DateTime.UtcNow;
    }
}
