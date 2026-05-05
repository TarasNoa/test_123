namespace Libr4.IDE.Application.WebSearch.DTOs;

/// <summary>
/// DTO for SearchResult
/// </summary>
public record SearchResultDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Snippet { get; init; } = string.Empty;
    public double RelevanceScore { get; init; }
    public string Provider { get; init; } = string.Empty;
}
