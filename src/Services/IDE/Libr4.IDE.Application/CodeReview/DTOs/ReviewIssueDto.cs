namespace Libr4.IDE.Application.CodeReview.DTOs;

/// <summary>
/// DTO for ReviewIssue
/// </summary>
public record ReviewIssueDto
{
    public Guid Id { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int LineNumber { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Suggestion { get; init; } = string.Empty;
    
    // Backward compatibility
    public string Type => Category;
    public string Description => Message;
    public string Recommendation => Suggestion;
}
