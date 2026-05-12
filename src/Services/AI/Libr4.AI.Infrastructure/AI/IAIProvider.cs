namespace Libr4.AI.Infrastructure.AI;

public interface IAIProvider
{
    string ProviderName { get; }
    Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null);
    Task<string> GenerateEmbeddingAsync(string text, string? model = null);
    Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null);
    Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null);
}
