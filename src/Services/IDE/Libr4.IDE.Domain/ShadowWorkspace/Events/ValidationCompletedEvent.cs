using Libr4.IDE.Domain.Common.Events;
using Libr4.IDE.Domain.ShadowWorkspace;

namespace Libr4.IDE.Domain.ShadowWorkspace.Events;

/// <summary>
/// Domain event raised when a validation is completed
/// </summary>
public class ValidationCompletedEvent : IDomainEvent
{
    public Guid ShadowWorkspaceId { get; }
    public string WorkspaceId { get; }
    public ValidationType ValidationType { get; }
    public bool Passed { get; }
    public DateTime OccurredOn { get; }
    
    public ValidationCompletedEvent(
        Guid shadowWorkspaceId,
        string workspaceId,
        ValidationType validationType,
        bool passed)
    {
        ShadowWorkspaceId = shadowWorkspaceId;
        WorkspaceId = workspaceId;
        ValidationType = validationType;
        Passed = passed;
        OccurredOn = DateTime.UtcNow;
    }
}
