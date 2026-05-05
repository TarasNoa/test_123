using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.GitHubBootstrap.Events;

/// <summary>
/// Domain event raised when project is seeded
/// </summary>
public class ProjectSeededEvent : IDomainEvent
{
    public Guid BootstrapProjectId { get; }
    public string ProjectId { get; }
    public int FilesCount { get; }
    public DateTime OccurredOn { get; }
    
    public ProjectSeededEvent(
        Guid bootstrapProjectId,
        string projectId,
        int filesCount)
    {
        BootstrapProjectId = bootstrapProjectId;
        ProjectId = projectId;
        FilesCount = filesCount;
        OccurredOn = DateTime.UtcNow;
    }
}
