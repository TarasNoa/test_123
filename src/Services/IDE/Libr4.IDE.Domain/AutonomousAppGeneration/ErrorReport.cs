namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Structured error produced by the fixer/semantic-blame agent after
/// analysing the console output of a failing iteration.
/// </summary>
public sealed class ErrorReport
{
    public string ErrorType { get; }
    public string Message { get; }
    public string? FilePath { get; }
    public int? LineNumber { get; }
    public string SuggestedFix { get; }
    public string? DiagnosingAgent { get; }

    public ErrorReport(
        string errorType,
        string message,
        string suggestedFix,
        string? filePath = null,
        int? lineNumber = null,
        string? diagnosingAgent = null)
    {
        ErrorType = errorType ?? "Unknown";
        Message = message ?? string.Empty;
        SuggestedFix = suggestedFix ?? string.Empty;
        FilePath = filePath;
        LineNumber = lineNumber;
        DiagnosingAgent = diagnosingAgent;
    }
}
