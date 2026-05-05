namespace Libr4.IDE.Application.HackerAgent.DTOs;

/// <summary>
/// DTO for HackerAgent
/// </summary>
public record HackerAgentDto
{
    public Guid Id { get; init; }
    public string OperationId { get; init; } = string.Empty;
    public string WorkspaceId { get; init; } = string.Empty;
    public List<SecurityScriptDto> Scripts { get; init; } = new();
    public List<GitHubSecurityToolDto> Tools { get; init; } = new();
    public List<string> TestResults { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
