namespace Libr4.IDE.Application.CodeIntelligence.DTOs;

/// <summary>
/// DTO for CodeIntelligence
/// </summary>
public record CodeIntelligenceDto
{
    public Guid Id { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public CompletionContextDto Context { get; init; } = null!;
    public List<CodeSuggestionDto> Suggestions { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
