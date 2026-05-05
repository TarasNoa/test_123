namespace Libr4.IDE.Application.SemanticCodeGraph.DTOs;

/// <summary>
/// DTO for CodeRelationship
/// </summary>
public record CodeRelationshipDto
{
    public Guid Id { get; init; }
    public Guid SourceEntityId { get; init; }
    public Guid TargetEntityId { get; init; }
    public string RelationshipType { get; init; } = string.Empty;
    public float Weight { get; init; }
}
