/*
using Libr4.IDE.Domain.Algorithms;
using Libr4.IDE.Domain.AI;
using Libr4.Shared.Kernel.Results;
using Libr4.IDE.Domain;

namespace Libr4.IDE.Application.AI.Algorithms;

public interface IAIAlgorithmService
{
    Task<Result<IntentDetectionResult>> DetectIntentAndEntitiesAsync(string message);
    Task<Result<float>> ScoreResponseQualityAsync(string userMessage, string aiResponse, Intent intent);
    string InferLanguage(string message, object? context = null);
    bool IsCodeRequest(string message);
}

// Result types for F# interop
public class IntentDetectionResult
{
    public Intent Intent { get; set; }
    public List<Entity> Entities { get; set; } = new();
    public float Confidence { get; set; }
}

public class Entity
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Start { get; set; }
    public int End { get; set; }
}
*/
