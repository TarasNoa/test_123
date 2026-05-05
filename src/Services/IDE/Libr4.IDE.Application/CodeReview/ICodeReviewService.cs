namespace Libr4.IDE.Application.CodeReview;

/// <summary>
/// Interface for code review service
/// </summary>
public interface ICodeReviewService
{
    Task<CodeReviewResult> ReviewAsync(string code, string language, CodeReviewOptions? options = null, CancellationToken ct = default);
    Task<CodeReviewResult> ReviewFileAsync(string filePath, CodeReviewOptions? options = null, CancellationToken ct = default);
    Task<CodeReviewResult> ReviewChangesAsync(string[] modifiedFiles, string baseBranch = "main", CancellationToken ct = default);
}

public class CodeReviewOptions
{
    public bool CheckStyle { get; set; } = true;
    public bool CheckSecurity { get; set; } = true;
    public bool CheckPerformance { get; set; } = true;
    public string? CustomRules { get; set; }
}

public class CodeReviewResult
{
    public bool Success { get; set; }
    public CodeReviewIssue[] Issues { get; set; } = Array.Empty<CodeReviewIssue>();
    public string[] Suggestions { get; set; } = Array.Empty<string>();
    public string Summary { get; set; } = string.Empty;
}

public class CodeReviewIssue
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Style", "Security", "Performance", "Bug"
    public string Severity { get; set; } = string.Empty; // "Info", "Warning", "Error", "Critical"
    public string Message { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
    public string? SuggestedFix { get; set; }
}
