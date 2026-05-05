using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.SeniorRolePrompts.Events;

namespace Libr4.IDE.Domain.SeniorRolePrompts;

/// <summary>
/// Domain entity representing a role prompt for a specific phase
/// </summary>
public class RolePrompt : AggregateRoot<Guid>
{
    public PhaseType PhaseType { get; private set; }
    public string PhaseName { get; private set; }
    public SeniorRole SeniorRole { get; private set; }
    public string SystemPrompt { get; private set; }
    public string UserPrompt { get; private set; }
    public DomainClass DomainClass { get; private set; }
    public bool RichMode { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    
    private RolePrompt() { }
    
    public RolePrompt(
        PhaseType phaseType,
        string phaseName,
        SeniorRole seniorRole,
        string systemPrompt,
        string userPrompt,
        DomainClass domainClass,
        bool richMode)
    {
        Id = Guid.NewGuid();
        PhaseType = phaseType;
        PhaseName = phaseName;
        SeniorRole = seniorRole;
        SystemPrompt = systemPrompt;
        UserPrompt = userPrompt;
        DomainClass = domainClass;
        RichMode = richMode;
        GeneratedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Marks the role prompt as generated and raises a domain event
    /// </summary>
    public void MarkAsGenerated()
    {
        AddDomainEvent(new RolePromptGeneratedEvent(Id, PhaseType, DomainClass));
    }
    
    public static RolePrompt Create(
        PhaseType phaseType,
        string phaseName,
        SeniorRole seniorRole,
        string systemPrompt,
        string userPrompt,
        DomainClass domainClass,
        bool richMode)
    {
        return new RolePrompt(phaseType, phaseName, seniorRole, systemPrompt, userPrompt, domainClass, richMode);
    }
}
