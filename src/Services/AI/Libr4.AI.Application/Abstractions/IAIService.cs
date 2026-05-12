namespace Libr4.AI.Application.Abstractions;

public interface IAIService
{
    Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null);
    Task<string> GenerateEmbeddingAsync(string text, string? model = null);
    Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null);
    Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null);
}
