using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.ShadowWorkspace.Events;

namespace Libr4.IDE.Domain.ShadowWorkspace;

/// <summary>
/// AggregateRoot for shadow workspace
/// </summary>
public class ShadowWorkspace : AggregateRoot<Guid>
{
    public string WorkspaceId { get; private set; }
    public string ParentWorkspaceId { get; private set; }
    public List<ShadowFile> Files { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private ShadowWorkspace() { }
    
    public ShadowWorkspace(
        string workspaceId,
        string parentWorkspaceId,
        List<ShadowFile>? files = null)
    {
        Id = Guid.NewGuid();
        WorkspaceId = workspaceId;
        ParentWorkspaceId = parentWorkspaceId;
        Files = files ?? new List<ShadowFile>();
        Status = "initializing";
        CreatedAt = DateTime.UtcNow;
        CompletedAt = null;
    }
    
    public void AddFile(ShadowFile file)
    {
        if (file != null)
        {
            Files.Add(file);
        }
    }
    
    public void SetStatus(string status)
    {
        Status = status;
        if (status == "completed" || status == "failed")
        {
            CompletedAt = DateTime.UtcNow;
        }
    }
    
    public ShadowFile? GetFileByPath(string filePath)
    {
        return Files.FirstOrDefault(f => f.FilePath == filePath);
    }
    
    /// <summary>
    /// Marks the workspace as created and raises a domain event
    /// </summary>
    public void MarkAsCreated()
    {
        AddDomainEvent(new WorkspaceCreatedEvent(Id, WorkspaceId));
    }
    
    /// <summary>
    /// Marks a validation as completed and raises a domain event
    /// </summary>
    public void MarkValidationCompleted(ValidationResult result)
    {
        AddDomainEvent(new ValidationCompletedEvent(Id, WorkspaceId, result.Type, result.Passed));
    }
    
    public static ShadowWorkspace Create(
        string workspaceId,
        string parentWorkspaceId,
        List<ShadowFile>? files = null)
    {
        return new ShadowWorkspace(workspaceId, parentWorkspaceId, files);
    }
}
