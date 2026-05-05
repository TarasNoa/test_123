using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.ShadowWorkspace.Events;

/// <summary>
/// Domain event raised when a file is added to shadow workspace
/// </summary>
public class FileAddedEvent : IDomainEvent
{
    public Guid ShadowWorkspaceId { get; }
    public string WorkspaceId { get; }
    public Guid FileId { get; }
    public string FilePath { get; }
    public DateTime OccurredOn { get; }
    
    public FileAddedEvent(
        Guid shadowWorkspaceId,
        string workspaceId,
        Guid fileId,
        string filePath)
    {
        ShadowWorkspaceId = shadowWorkspaceId;
        WorkspaceId = workspaceId;
        FileId = fileId;
        FilePath = filePath;
        OccurredOn = DateTime.UtcNow;
    }
}
