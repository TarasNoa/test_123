using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.SeniorRolePrompts.Events;

/// <summary>
/// Domain event raised when a role prompt is successfully generated
/// </summary>
public class RolePromptGeneratedEvent : IDomainEvent
{
    public Guid RolePromptId { get; }
    public PhaseType PhaseType { get; }
    public DomainClass DomainClass { get; }
    public DateTime OccurredOn { get; }
    
    public RolePromptGeneratedEvent(
        Guid rolePromptId,
        PhaseType phaseType,
        DomainClass domainClass)
    {
        RolePromptId = rolePromptId;
        PhaseType = phaseType;
        DomainClass = domainClass;
        OccurredOn = DateTime.UtcNow;
    }
}
