namespace Libr4.IDE.Application.CodeIntelligence.DTOs;

/// <summary>
/// DTO for CodeSuggestion
/// </summary>
public record CodeSuggestionDto
{
    public Guid Id { get; init; }
    public string SuggestionText { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public double RelevanceScore { get; init; }
    public string Description { get; init; } = string.Empty;
}
