namespace Libr4.IDE.Application.HackerAgent.DTOs;

/// <summary>
/// DTO for GitHubSecurityTool
/// </summary>
public record GitHubSecurityToolDto
{
    public Guid Id { get; init; }
    public string RepoName { get; init; } = string.Empty;
    public string RepoUrl { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ToolType { get; init; } = string.Empty;
    public int Stars { get; init; }
}
