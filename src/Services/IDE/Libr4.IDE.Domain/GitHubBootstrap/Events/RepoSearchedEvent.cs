using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.GitHubBootstrap.Events;

/// <summary>
/// Domain event raised when repository is searched
/// </summary>
public class RepoSearchedEvent : IDomainEvent
{
    public Guid BootstrapProjectId { get; }
    public string ProjectId { get; }
    public DateTime OccurredOn { get; }
    
    public RepoSearchedEvent(
        Guid bootstrapProjectId,
        string projectId)
    {
        BootstrapProjectId = bootstrapProjectId;
        ProjectId = projectId;
        OccurredOn = DateTime.UtcNow;
    }
}
