namespace Libr4.AI.Application.AgentExecution;

public record ErrorAnalysis
{
    public string ErrorType { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string? SuggestedFix { get; init; }
    public string? FixDescription { get; init; }
    public double Confidence { get; init; }
}

public interface ICodeErrorAnalyzer
{
    ErrorAnalysis AnalyzeError(string errorMessage, string code, string language);
}
