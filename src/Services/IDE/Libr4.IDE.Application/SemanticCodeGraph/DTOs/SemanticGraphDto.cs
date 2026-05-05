namespace Libr4.IDE.Application.SemanticCodeGraph.DTOs;

/// <summary>
/// DTO for SemanticGraph
/// </summary>
public record SemanticGraphDto
{
    public Guid Id { get; init; }
    public string GraphId { get; init; } = string.Empty;
    public string WorkspaceId { get; init; } = string.Empty;
    public List<CodeEntityDto> Entities { get; init; } = new();
    public List<CodeRelationshipDto> Relationships { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
