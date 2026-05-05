using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.ShadowWorkspace.Events;

/// <summary>
/// Domain event raised when a shadow workspace is created
/// </summary>
public class WorkspaceCreatedEvent : IDomainEvent
{
    public Guid ShadowWorkspaceId { get; }
    public string WorkspaceId { get; }
    public DateTime OccurredOn { get; }
    
    public WorkspaceCreatedEvent(
        Guid shadowWorkspaceId,
        string workspaceId)
    {
        ShadowWorkspaceId = shadowWorkspaceId;
        WorkspaceId = workspaceId;
        OccurredOn = DateTime.UtcNow;
    }
}
