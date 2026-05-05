namespace Libr4.IDE.Application.WebSearch.DTOs;

/// <summary>
/// DTO for WebSearch
/// </summary>
public record WebSearchDto
{
    public Guid Id { get; init; }
    public string SearchId { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public List<SearchResultDto> Results { get; init; } = new();
    public List<string> ProvidersUsed { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
