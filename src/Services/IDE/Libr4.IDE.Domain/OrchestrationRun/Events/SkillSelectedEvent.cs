using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.OrchestrationRun.Events;

/// <summary>
/// Domain event raised when a skill is selected for orchestration
/// </summary>
public class SkillSelectedEvent : IDomainEvent
{
    public Guid OrchestrationRunId { get; }
    public string RunId { get; }
    public string SkillType { get; }
    public string SkillName { get; }
    public DateTime OccurredOn { get; }
    
    public SkillSelectedEvent(
        Guid orchestrationRunId,
        string runId,
        string skillType,
        string skillName)
    {
        OrchestrationRunId = orchestrationRunId;
        RunId = runId;
        SkillType = skillType;
        SkillName = skillName;
        OccurredOn = DateTime.UtcNow;
    }
}
