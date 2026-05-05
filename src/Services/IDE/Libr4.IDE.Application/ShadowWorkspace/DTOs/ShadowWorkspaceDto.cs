namespace Libr4.IDE.Application.ShadowWorkspace.DTOs;

/// <summary>
/// DTO for ShadowWorkspace
/// </summary>
public record ShadowWorkspaceDto
{
    public Guid Id { get; init; }
    public string WorkspaceId { get; init; } = string.Empty;
    public string ParentWorkspaceId { get; init; } = string.Empty;
    public List<ShadowFileDto> Files { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
