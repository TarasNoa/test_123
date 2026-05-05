namespace Libr4.IDE.Application.GitHubBootstrap.DTOs;

/// <summary>
/// DTO for GitHubRepo
/// </summary>
public record GitHubRepoDto
{
    public Guid Id { get; init; }
    public string RepoName { get; init; } = string.Empty;
    public string Owner { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string License { get; init; } = string.Empty;
    public int Stars { get; init; }
    public string Url { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
}
