namespace Libr4.IDE.Application.AI;

/// <summary>
/// Interface for AI algorithm service
/// </summary>
public interface IAIAlgorithmService
{
    Task<string> OptimizeAsync(string input, string algorithm, CancellationToken ct = default);
    Task<T?> AnalyzeAsync<T>(string data, string analysisType, CancellationToken ct = default);
    Task<string[]> GetAvailableAlgorithmsAsync();
}
