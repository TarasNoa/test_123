namespace Libr4.IDE.Application.SemanticCodeGraph.DTOs;

/// <summary>
/// DTO for CodeEntity
/// </summary>
public record CodeEntityDto
{
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public float[] Embedding { get; init; } = Array.Empty<float>();
    public Dictionary<string, object> Metadata { get; init; } = new();
}
