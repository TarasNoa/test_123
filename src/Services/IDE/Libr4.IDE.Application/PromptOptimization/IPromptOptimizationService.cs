namespace Libr4.IDE.Application.PromptOptimization;

/// <summary>
/// Interface for prompt optimization service
/// </summary>
public interface IPromptOptimizationService
{
    Task<string> OptimizeAsync(string prompt, string model = "default", CancellationToken ct = default);
    Task<string[]> SuggestVariantsAsync(string basePrompt, int count = 3, CancellationToken ct = default);
    Task<PromptAnalysis> AnalyzeAsync(string prompt, CancellationToken ct = default);
}

public class PromptAnalysis
{
    public int TokenCount { get; set; }
    public double ComplexityScore { get; set; }
    public string[] SuggestedImprovements { get; set; } = Array.Empty<string>();
    public bool IsWellFormed { get; set; }
}
