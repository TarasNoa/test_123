namespace Libr4.IDE.Application.GitHubBootstrap.DTOs;

/// <summary>
/// DTO for BootstrapProject
/// </summary>
public record BootstrapProjectDto
{
    public Guid Id { get; init; }
    public string ProjectId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public GitHubRepoDto? SelectedTemplate { get; init; }
    public List<string> FilesCreated { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
