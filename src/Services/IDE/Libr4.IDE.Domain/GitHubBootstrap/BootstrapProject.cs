using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.GitHubBootstrap.Events;

namespace Libr4.IDE.Domain.GitHubBootstrap;

/// <summary>
/// AggregateRoot for bootstrap project
/// </summary>
public class BootstrapProject : AggregateRoot<Guid>
{
    public string ProjectId { get; private set; }
    public string ProjectName { get; private set; }
    public GitHubRepo SelectedTemplate { get; private set; }
    public List<string> FilesCreated { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private BootstrapProject() { }
    
    public BootstrapProject(
        string projectId,
        string projectName,
        GitHubRepo? selectedTemplate = null)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        ProjectName = projectName;
        SelectedTemplate = selectedTemplate!;
        FilesCreated = new List<string>();
        Status = "initializing";
        CreatedAt = DateTime.UtcNow;
        CompletedAt = null;
    }
    
    public void SetTemplate(GitHubRepo template)
    {
        SelectedTemplate = template;
    }
    
    public void AddFileCreated(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            FilesCreated.Add(filePath);
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
    
    /// <summary>
    /// Marks the project as searched and raises a domain event
    /// </summary>
    public void MarkRepoSearched()
    {
        AddDomainEvent(new RepoSearchedEvent(Id, ProjectId));
    }
    
    /// <summary>
    /// Marks the license as checked and raises a domain event
    /// </summary>
    public void MarkLicenseChecked(LicenseType license)
    {
        AddDomainEvent(new LicenseCheckedEvent(Id, ProjectId, license));
    }
    
    /// <summary>
    /// Marks the project as seeded and raises a domain event
    /// </summary>
    public void MarkProjectSeeded()
    {
        AddDomainEvent(new ProjectSeededEvent(Id, ProjectId, FilesCreated.Count));
    }
    
    public static BootstrapProject Create(
        string projectId,
        string projectName,
        GitHubRepo? selectedTemplate = null)
    {
        return new BootstrapProject(projectId, projectName, selectedTemplate);
    }
}
