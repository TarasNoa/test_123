namespace Libr4.IDE.Application.AgentMemorySystem.DTOs;

/// <summary>
/// DTO for MemoryFragment
/// </summary>
public record MemoryFragmentDto
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
    public float RelevanceScore { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
