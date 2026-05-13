using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Libr4.IDE.Application.AI;

/// <summary>
/// AI-powered algorithm service wrapper using LLM for code optimization and analysis.
/// </summary>
public class AIAlgorithmServiceWrapper : IAIAlgorithmService
{
    private readonly IAIService _aiService;
    private readonly ILogger<AIAlgorithmServiceWrapper> _logger;

    public AIAlgorithmServiceWrapper(
        IAIService aiService,
        ILogger<AIAlgorithmServiceWrapper> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<string> OptimizeAsync(string input, string algorithm, CancellationToken ct = default)
    {
        _logger.LogInformation("AI optimizing with {Algorithm} on input ({Length} chars)", algorithm, input.Length);

        var systemPrompt = $"You are an expert code optimizer. Apply the '{algorithm}' optimization to the provided code. Return ONLY the optimized code without explanations.";
        var prompt = $"Optimize the following code using {algorithm}:\n\n```{input}\n```";

        var result = await _aiService.GenerateCompletionAsync(prompt, systemPrompt, null);
        return result?.Trim() ?? input;
    }

    public async Task<T?> AnalyzeAsync<T>(string data, string analysisType, CancellationToken ct = default)
    {
        _logger.LogInformation("AI {AnalysisType} analysis on input ({Length} chars)", analysisType, data.Length);

        var systemPrompt = $"You are an expert code analyst. Perform a {analysisType} analysis and return the result as valid JSON that can deserialize to the expected type.";
        var prompt = $"Analyze the following data with {analysisType}:\n\n{data}\n\nReturn the result as JSON.";

        var result = await _aiService.GenerateCompletionAsync(prompt, systemPrompt, null);
        if (string.IsNullOrWhiteSpace(result))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(result.Trim());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize AI analysis result to {Type}", typeof(T).Name);
            return default;
        }
    }

    public Task<string[]> GetAvailableAlgorithmsAsync()
    {
        return Task.FromResult(new[]
        {
            "performance",
            "readability",
            "security",
            "complexity",
            "memory",
            "parallelization"
        });
    }
}
