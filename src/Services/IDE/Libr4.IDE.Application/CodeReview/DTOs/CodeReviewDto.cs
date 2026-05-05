namespace Libr4.IDE.Application.CodeReview.DTOs;

/// <summary>
/// DTO for CodeReview
/// </summary>
public record CodeReviewDto
{
    public Guid Id { get; init; }
    public string ReviewId { get; init; } = string.Empty;
    public string WorkspaceId { get; init; } = string.Empty;
    public List<string> Files { get; init; } = new();
    public List<ReviewIssueDto> Issues { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
