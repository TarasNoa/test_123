namespace Libr4.IDE.Application.SemanticBlame.DTOs;

/// <summary>
/// DTO for SemanticBlame
/// </summary>
public record SemanticBlameDto
{
    public Guid Id { get; init; }
    public string BlameId { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public List<BlameEntryDto> Entries { get; init; } = new();
    public CodeEvolutionDto? Evolution { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
