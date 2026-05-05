using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AI;

/// <summary>
/// Stub implementation of AI algorithm service
/// </summary>
public class AIAlgorithmServiceWrapper : IAIAlgorithmService
{
    private readonly ILogger<AIAlgorithmServiceWrapper> _logger;

    public AIAlgorithmServiceWrapper(ILogger<AIAlgorithmServiceWrapper> logger)
    {
        _logger = logger;
    }

    public Task<string> OptimizeAsync(string input, string algorithm, CancellationToken ct = default)
    {
        _logger.LogInformation("Running algorithm {Algorithm} on input ({Length} chars)", algorithm, input.Length);
        return Task.FromResult(input); // Return unchanged
    }

    public Task<T?> AnalyzeAsync<T>(string data, string analysisType, CancellationToken ct = default)
    {
        _logger.LogInformation("Running {AnalysisType} analysis", analysisType);
        return Task.FromResult<T?>(default);
    }

    public Task<string[]> GetAvailableAlgorithmsAsync()
    {
        return Task.FromResult(new[] { "optimize", "analyze", "transform" });
    }
}
