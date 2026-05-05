using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure;

public class ModelCapabilities
{
    public string ModelId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public double CostPer1KTokens { get; set; }
    public int MaxTokens { get; set; }
    public double LatencyMs { get; set; }
    public List<string> SupportedFeatures { get; set; } = new();
    public double CodingScore { get; set; }
    public double ReasoningScore { get; set; }
}

public class RoutingDecision
{
    public string ModelId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public double EstimatedCost { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, double> Scores { get; set; } = new();
    public bool ConsensusReached { get; set; }
    public Dictionary<string, string> ProviderResponses { get; set; } = new();
    public double AgreementPercentage { get; set; }
}

public class LLMRouter
{
    private readonly List<ModelCapabilities> _models;
    private readonly ILogger<LLMRouter> _logger;
    private readonly double _consensusThreshold = 0.75;

    public LLMRouter(ILogger<LLMRouter> logger)
    {
        _logger = logger;
        _models = LoadModelCapabilities();
    }

    private List<ModelCapabilities> LoadModelCapabilities()
    {
        return new List<ModelCapabilities>
        {
            new ModelCapabilities
            {
                ModelId = "gpt-4o",
                Provider = "openai",
                CostPer1KTokens = 0.005,
                MaxTokens = 128000,
                LatencyMs = 500,
                SupportedFeatures = new List<string> { "coding", "reasoning", "vision", "tools" },
                CodingScore = 0.95,
                ReasoningScore = 0.95
            },
            new ModelCapabilities
            {
                ModelId = "gpt-4o-mini",
                Provider = "openai",
                CostPer1KTokens = 0.00015,
                MaxTokens = 128000,
                LatencyMs = 200,
                SupportedFeatures = new List<string> { "coding", "reasoning", "vision", "tools" },
                CodingScore = 0.85,
                ReasoningScore = 0.80
            },
            new ModelCapabilities
            {
                ModelId = "claude-3-5-sonnet-20241022",
                Provider = "anthropic",
                CostPer1KTokens = 0.003,
                MaxTokens = 200000,
                LatencyMs = 400,
                SupportedFeatures = new List<string> { "coding", "reasoning", "vision", "tools" },
                CodingScore = 0.92,
                ReasoningScore = 0.93
            },
            new ModelCapabilities
            {
                ModelId = "claude-3-5-haiku-20241022",
                Provider = "anthropic",
                CostPer1KTokens = 0.00025,
                MaxTokens = 200000,
                LatencyMs = 150,
                SupportedFeatures = new List<string> { "coding", "reasoning", "vision", "tools" },
                CodingScore = 0.78,
                ReasoningScore = 0.75
            },
            new ModelCapabilities
            {
                ModelId = "deepseek-coder",
                Provider = "deepseek",
                CostPer1KTokens = 0.00014,
                MaxTokens = 128000,
                LatencyMs = 300,
                SupportedFeatures = new List<string> { "coding", "reasoning" },
                CodingScore = 0.88,
                ReasoningScore = 0.70
            }
        };
    }

    public RoutingDecision Route(string task, string context, List<string> requiredFeatures, double maxCost)
    {
        var scores = new Dictionary<string, double>();

        foreach (var model in _models)
        {
            var featureScore = requiredFeatures.All(f => model.SupportedFeatures.Contains(f)) ? 1.0 : 0.0;
            var complexityScore = CalculateComplexityScore(task, context, model);
            var costScore = 1.0 - (model.CostPer1KTokens / _models.Max(m => m.CostPer1KTokens));
            var latencyScore = 1.0 - (model.LatencyMs / _models.Max(m => m.LatencyMs));
            var totalScore = 0.4 * featureScore + 0.3 * complexityScore + 0.2 * costScore + 0.1 * latencyScore;
            scores[model.ModelId] = totalScore;
        }

        var bestModel = scores.OrderByDescending(s => s.Value).First();
        var selectedModel = _models.First(m => m.ModelId == bestModel.Key);

        var estimatedTokens = EstimateTokens(task + context);
        var estimatedCost = (estimatedTokens / 1000.0) * selectedModel.CostPer1KTokens;

        if (estimatedCost > maxCost)
        {
            _logger.LogWarning("Estimated cost {EstimatedCost} exceeds max cost {MaxCost}, selecting cheaper model", estimatedCost, maxCost);
            var cheaperModel = _models.Where(m => m.CostPer1KTokens < selectedModel.CostPer1KTokens)
                .OrderBy(m => m.CostPer1KTokens)
                .FirstOrDefault();
            
            if (cheaperModel != null)
            {
                selectedModel = cheaperModel;
                estimatedCost = (estimatedTokens / 1000.0) * selectedModel.CostPer1KTokens;
            }
        }

        var consensusResult = CheckMultiProviderConsensus(task, context, requiredFeatures);

        _logger.LogInformation("Routed to model {ModelId} with confidence {Confidence}, consensus: {ConsensusReached} ({AgreementPercentage}%)",
            selectedModel.ModelId, bestModel.Value, consensusResult.ConsensusReached, consensusResult.AgreementPercentage * 100);

        return new RoutingDecision
        {
            ModelId = selectedModel.ModelId,
            Provider = selectedModel.Provider,
            EstimatedCost = estimatedCost,
            Confidence = bestModel.Value,
            Scores = scores,
            ConsensusReached = consensusResult.ConsensusReached,
            ProviderResponses = consensusResult.ProviderResponses,
            AgreementPercentage = consensusResult.AgreementPercentage
        };
    }

    private (bool ConsensusReached, Dictionary<string, string> ProviderResponses, double AgreementPercentage) CheckMultiProviderConsensus(
        string task, string context, List<string> requiredFeatures)
    {
        var providerResponses = new Dictionary<string, string>();
        var topModelsByProvider = _models
            .GroupBy(m => m.Provider)
            .Select(g => g.OrderByDescending(m => m.CodingScore + m.ReasoningScore).First())
            .ToList();

        foreach (var model in topModelsByProvider)
        {
            var simulatedResponse = SimulateProviderResponse(model, task, context);
            providerResponses[model.Provider] = simulatedResponse;
        }

        var agreementCount = 0;
        var comparisons = 0;

        var providers = providerResponses.Keys.ToList();
        for (int i = 0; i < providers.Count; i++)
        {
            for (int j = i + 1; j < providers.Count; j++)
            {
                comparisons++;
                if (CalculateResponseSimilarity(providerResponses[providers[i]], providerResponses[providers[j]]) > 0.7)
                {
                    agreementCount++;
                }
            }
        }

        var agreementPercentage = comparisons > 0 ? (double)agreementCount / comparisons : 0.0;
        var consensusReached = agreementPercentage >= _consensusThreshold;

        _logger.LogInformation("Multi-provider consensus check: {AgreementCount}/{Comparisons} agreements ({Percentage}%)",
            agreementCount, comparisons, agreementPercentage * 100);

        return (consensusReached, providerResponses, agreementPercentage);
    }

    private string SimulateProviderResponse(ModelCapabilities model, string task, string context)
    {
        var qualityScore = (model.CodingScore + model.ReasoningScore) / 2.0;
        
        return model.Provider switch
        {
            "openai" => $"OpenAI response: {task.Substring(0, Math.Min(task.Length, 50))}...",
            "anthropic" => $"Anthropic response: {task.Substring(0, Math.Min(task.Length, 50))}...",
            "deepseek" => $"DeepSeek response: {task.Substring(0, Math.Min(task.Length, 50))}...",
            _ => $"Generic response: {task.Substring(0, Math.Min(task.Length, 50))}..."
        };
    }

    private double CalculateResponseSimilarity(string response1, string response2)
    {
        var words1 = response1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var words2 = response2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var commonWords = words1.Intersect(words2).Count();
        var totalWords = words1.Union(words2).Count();
        
        return totalWords > 0 ? (double)commonWords / totalWords : 0.0;
    }

    private double CalculateComplexityScore(string task, string context, ModelCapabilities model)
    {
        var taskComplexity = task.Length / 1000.0;
        var contextComplexity = context.Length / 10000.0;
        var totalComplexity = taskComplexity + contextComplexity;

        var normalizedComplexity = Math.Min(totalComplexity / 10.0, 1.0);
        var modelCapability = (model.CodingScore + model.ReasoningScore) / 2.0;

        return normalizedComplexity * modelCapability + (1 - normalizedComplexity) * (1 - modelCapability);
    }

    private int EstimateTokens(string text)
    {
        return text.Length / 4;
    }

    public List<ModelCapabilities> GetAllModels()
    {
        return _models;
    }

    public ModelCapabilities? GetModel(string modelId)
    {
        return _models.FirstOrDefault(m => m.ModelId == modelId);
    }
}
