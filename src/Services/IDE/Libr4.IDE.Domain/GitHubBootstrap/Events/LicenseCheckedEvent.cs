using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.GitHubBootstrap.Events;

/// <summary>
/// Domain event raised when license is checked
/// </summary>
public class LicenseCheckedEvent : IDomainEvent
{
    public Guid BootstrapProjectId { get; }
    public string ProjectId { get; }
    public LicenseType License { get; }
    public DateTime OccurredOn { get; }
    
    public LicenseCheckedEvent(
        Guid bootstrapProjectId,
        string projectId,
        LicenseType license)
    {
        BootstrapProjectId = bootstrapProjectId;
        ProjectId = projectId;
        License = license;
        OccurredOn = DateTime.UtcNow;
    }
}
