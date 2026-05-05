namespace Libr4.IDE.Application.AgentMemorySystem.DTOs;

/// <summary>
/// DTO for AgentMemory
/// </summary>
public record AgentMemoryDto
{
    public Guid Id { get; init; }
    public string MemoryId { get; init; } = string.Empty;
    public string AgentId { get; init; } = string.Empty;
    public List<MemoryFragmentDto> Fragments { get; init; } = new();
    public string CompressionLevel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastCompressedAt { get; init; }
}
