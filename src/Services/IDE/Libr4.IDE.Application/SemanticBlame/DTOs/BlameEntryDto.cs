namespace Libr4.IDE.Application.SemanticBlame.DTOs;

/// <summary>
/// DTO for BlameEntry
/// </summary>
public record BlameEntryDto
{
    public Guid Id { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int LineNumber { get; init; }
    public string Author { get; init; } = string.Empty;
    public string CommitHash { get; init; } = string.Empty;
    public DateTime CommitDate { get; init; }
    public string CommitMessage { get; init; } = string.Empty;
}
