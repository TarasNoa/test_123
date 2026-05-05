/*
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace Libr4.Gateway;

/// <summary>
/// AI-Driven Rate Limiting
/// Gateway asks AI: "Is this attack or confused user?"
/// AI analyzes pattern and decides: Ban for 1 hour or limit to 1 req/sec
/// </summary>
public interface IAiDrivenRateLimiter
{
    /// <summary>
    /// Analyze request pattern and decide on rate limiting action
    /// </summary>
    Task<RateLimitDecision> AnalyzeAndDecideAsync(
        string clientId,
        string path,
        RequestPattern pattern,
        CancellationToken ct = default);

    /// <summary>
    /// Learn from feedback (false positives, successful blocks)
    /// </summary>
    Task LearnFromFeedbackAsync(RateLimitFeedback feedback, CancellationToken ct = default);

    /// <summary>
    /// Get current risk score for client
    /// </summary>
    Task<RiskScore> GetClientRiskScoreAsync(string clientId, CancellationToken ct = default);
}

/// <summary>
/// Implementation using ML.NET for pattern recognition
/// </summary>
public class AiDrivenRateLimiter : IAiDrivenRateLimiter
{
    private readonly ILogger<AiDrivenRateLimiter> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache _cache;
    private readonly IRateLimitStore _store;
    private readonly IConfiguration _configuration;
    
    // ML.NET components
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<RequestFeatures, AttackPrediction>? _predictionEngine;

    // Pattern history
    private readonly Dictionary<string, ClientPatternHistory> _patternHistories = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public AiDrivenRateLimiter(
        ILogger<AiDrivenRateLimiter> logger,
        IHttpClientFactory httpClientFactory,
        IDistributedCache cache,
        IRateLimitStore store,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _store = store;
        _configuration = configuration;
        _mlContext = new MLContext(seed: 42);
        
        // Initialize or load model
        InitializeModel();
    }

    public async Task<RateLimitDecision> AnalyzeAndDecideAsync(
        string clientId,
        string path,
        RequestPattern pattern,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Analyzing request pattern for client {ClientId} to {Path}",
            clientId, path);

        // Extract features
        var features = ExtractFeatures(clientId, path, pattern);
        
        // ML prediction
        var prediction = PredictAttack(features);
        
        // Get historical context
        var history = GetOrCreateHistory(clientId);
        
        // Calculate risk score
        var riskScore = CalculateRiskScore(prediction, features, history);
        
        // Make decision based on risk score
        var decision = MakeDecision(clientId, riskScore, features, prediction);
        
        // Update history
        UpdateHistory(clientId, pattern, decision);
        
        // Log decision
        _logger.LogInformation(
            "Rate limit decision for {ClientId}: {Action} (risk: {Risk:F2}, confidence: {Confidence:F2})",
            clientId, decision.Action, riskScore.Score, prediction.Confidence);

        return decision;
    }

    public async Task LearnFromFeedbackAsync(RateLimitFeedback feedback, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Learning from feedback: {Action} was {Correct}",
            feedback.Decision, feedback.WasCorrect ? "correct" : "incorrect");

        // Retrain model with new data point
        var trainingData = _mlContext.Data.LoadFromEnumerable(new[]
        {
            new RequestFeatures
            {
                RequestCount = feedback.Pattern.RequestCount,
                ErrorRate = feedback.Pattern.ErrorRate,
                UniquePaths = feedback.Pattern.UniquePaths,
                TimeWindow = feedback.Pattern.TimeWindow,
                Burstiness = feedback.Pattern.Burstiness,
                IsAttack = feedback.WasAttack  // Label
            }
        });

        // Online learning - add to existing model
        // In production, batch these updates
        var updatedModel = _mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: nameof(RequestFeatures.IsAttack),
                featureColumnName: nameof(RequestFeatures.Features))
            .Fit(trainingData);

        _model = updatedModel;
        _predictionEngine = _mlContext.Model
            .CreatePredictionEngine<RequestFeatures, AttackPrediction>(_model);

        // Store feedback for future batch training
        await _store.StoreFeedbackAsync(feedback, ct);
    }

    public async Task<RiskScore> GetClientRiskScoreAsync(string clientId, CancellationToken ct = default)
    {
        var cached = await _cache.GetStringAsync($"risk:{clientId}", ct);
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<RiskScore>(cached)!;
        }

        var history = GetOrCreateHistory(clientId);
        var score = CalculateAggregatedRiskScore(history);
        
        await _cache.SetStringAsync(
            $"risk:{clientId}",
            JsonSerializer.Serialize(score),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            ct);

        return score;
    }

    // Private implementation methods

    private void InitializeModel()
    {
        // Create initial model with some synthetic training data
        // In production, load pre-trained model from disk
        var trainingData = _mlContext.Data.LoadFromEnumerable(GetInitialTrainingData());

        var pipeline = _mlContext.Transforms
            .Concatenate("Features",
                nameof(RequestFeatures.RequestCount),
                nameof(RequestFeatures.ErrorRate),
                nameof(RequestFeatures.UniquePaths),
                nameof(RequestFeatures.TimeWindow),
                nameof(RequestFeatures.Burstiness))
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: nameof(RequestFeatures.IsAttack),
                featureColumnName: "Features",
                numberOfLeaves: 20,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 10));

        _model = pipeline.Fit(trainingData);
        _predictionEngine = _mlContext.Model
            .CreatePredictionEngine<RequestFeatures, AttackPrediction>(_model);
    }

    private IEnumerable<RequestFeatures> GetInitialTrainingData()
    {
        // Synthetic training data
        // Normal user patterns
        yield return new RequestFeatures { RequestCount = 10, ErrorRate = 0.1f, UniquePaths = 5, TimeWindow = 60, Burstiness = 0.3f, IsAttack = false };
        yield return new RequestFeatures { RequestCount = 30, ErrorRate = 0.05f, UniquePaths = 10, TimeWindow = 60, Burstiness = 0.5f, IsAttack = false };
        yield return new RequestFeatures { RequestCount = 50, ErrorRate = 0.08f, UniquePaths = 15, TimeWindow = 60, Burstiness = 0.4f, IsAttack = false };
        
        // Attack patterns
        yield return new RequestFeatures { RequestCount = 500, ErrorRate = 0.8f, UniquePaths = 200, TimeWindow = 10, Burstiness = 0.95f, IsAttack = true };
        yield return new RequestFeatures { RequestCount = 1000, ErrorRate = 0.9f, UniquePaths = 50, TimeWindow = 5, Burstiness = 0.99f, IsAttack = true };
        yield return new RequestFeatures { RequestCount = 200, ErrorRate = 0.5f, UniquePaths = 100, TimeWindow = 15, Burstiness = 0.8f, IsAttack = true };
    }

    private RequestFeatures ExtractFeatures(string clientId, string path, RequestPattern pattern)
    {
        return new RequestFeatures
        {
            RequestCount = pattern.RequestCount,
            ErrorRate = pattern.ErrorRate,
            UniquePaths = pattern.UniquePaths,
            TimeWindow = pattern.TimeWindow,
            Burstiness = pattern.Burstiness,
            IsAttack = false  // Will be predicted
        };
    }

    private AttackPrediction PredictAttack(RequestFeatures features)
    {
        if (_predictionEngine == null)
        {
            return new AttackPrediction { IsAttack = false, Confidence = 0.5f };
        }

        return _predictionEngine.Predict(features);
    }

    private RiskScore CalculateRiskScore(
        AttackPrediction prediction,
        RequestFeatures features,
        ClientPatternHistory history)
    {
        var baseScore = prediction.IsAttack ? 0.7f : 0.3f;
        var confidenceFactor = prediction.Confidence;
        
        // Adjust based on historical behavior
        var historyFactor = Math.Min(history.RecentViolations * 0.1f, 0.3f);
        
        // Adjust based on burstiness
        var burstFactor = features.Burstiness * 0.2f;
        
        var finalScore = Math.Min(1.0f, baseScore * confidenceFactor + historyFactor + burstFactor);
        
        return new RiskScore
        {
            ClientId = history.ClientId,
            Score = finalScore,
            Confidence = prediction.Confidence,
            Factors = new Dictionary<string, float>
            {
                ["ml_prediction"] = baseScore,
                ["confidence"] = confidenceFactor,
                ["history"] = historyFactor,
                ["burstiness"] = burstFactor
            }
        };
    }

    private RateLimitDecision MakeDecision(
        string clientId,
        RiskScore riskScore,
        RequestFeatures features,
        AttackPrediction prediction)
    {
        // Decision thresholds
        if (riskScore.Score >= 0.9f)
        {
            return new RateLimitDecision
            {
                ClientId = clientId,
                Action = RateLimitAction.Ban,
                Duration = TimeSpan.FromHours(1),
                Reason = "High confidence attack detection",
                RiskScore = riskScore.Score,
                Limit = 0,
                SuggestedResponse = "Return 403 Forbidden"
            };
        }
        
        if (riskScore.Score >= 0.7f)
        {
            return new RateLimitDecision
            {
                ClientId = clientId,
                Action = RateLimitAction.StrictLimit,
                Duration = TimeSpan.FromMinutes(30),
                Reason = "Suspicious behavior pattern",
                RiskScore = riskScore.Score,
                Limit = 1,  // 1 request per second
                SuggestedResponse = "Apply strict rate limit with 429"
            };
        }
        
        if (riskScore.Score >= 0.5f)
        {
            return new RateLimitDecision
            {
                ClientId = clientId,
                Action = RateLimitAction.Warn,
                Duration = TimeSpan.FromMinutes(15),
                Reason = "Elevated activity",
                RiskScore = riskScore.Score,
                Limit = 10,  // 10 requests per second
                SuggestedResponse = "Apply standard rate limit with monitoring"
            };
        }
        
        if (riskScore.Score >= 0.3f)
        {
            return new RateLimitDecision
            {
                ClientId = clientId,
                Action = RateLimitAction.Monitor,
                Duration = TimeSpan.FromMinutes(5),
                Reason = "Slightly elevated, monitoring",
                RiskScore = riskScore.Score,
                Limit = 30,
                SuggestedResponse = "Allow but watch closely"
            };
        }
        
        return new RateLimitDecision
        {
            ClientId = clientId,
            Action = RateLimitAction.Allow,
            Duration = TimeSpan.Zero,
            Reason = "Normal behavior",
            RiskScore = riskScore.Score,
            Limit = 100,  // Normal limit
            SuggestedResponse = "Process normally"
        };
    }

    private ClientPatternHistory GetOrCreateHistory(string clientId)
    {
        _lock.EnterReadLock();
        try
        {
            if (_patternHistories.TryGetValue(clientId, out var history))
            {
                return history;
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        _lock.EnterWriteLock();
        try
        {
            if (!_patternHistories.ContainsKey(clientId))
            {
                _patternHistories[clientId] = new ClientPatternHistory
                {
                    ClientId = clientId,
                    FirstSeen = DateTime.UtcNow,
                    RequestCount = 0,
                    RecentViolations = 0
                };
            }
            return _patternHistories[clientId];
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void UpdateHistory(string clientId, RequestPattern pattern, RateLimitDecision decision)
    {
        _lock.EnterWriteLock();
        try
        {
            var history = _patternHistories[clientId];
            history.LastSeen = DateTime.UtcNow;
            history.RequestCount += pattern.RequestCount;
            
            if (decision.Action != RateLimitAction.Allow)
            {
                history.RecentViolations++;
                history.LastViolation = DateTime.UtcNow;
            }
            
            // Decay old violations
            if (history.LastViolation < DateTime.UtcNow.AddHours(-1))
            {
                history.RecentViolations = Math.Max(0, history.RecentViolations - 1);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private RiskScore CalculateAggregatedRiskScore(ClientPatternHistory history)
    {
        var timeSinceLastViolation = DateTime.UtcNow - history.LastViolation;
        var recencyPenalty = timeSinceLastViolation.TotalHours < 1 ? 0.2f : 0f;
        
        var score = Math.Min(1.0f, 
            (history.RecentViolations * 0.15f) + recencyPenalty);

        return new RiskScore
        {
            ClientId = history.ClientId,
            Score = score,
            Confidence = 0.8f,
            Factors = new Dictionary<string, float>
            {
                ["recent_violations"] = history.RecentViolations * 0.15f,
                ["recency"] = recencyPenalty
            }
        };
    }
}

// ML.NET data classes
public class RequestFeatures
{
    public float RequestCount { get; set; }
    public float ErrorRate { get; set; }
    public float UniquePaths { get; set; }
    public float TimeWindow { get; set; }
    public float Burstiness { get; set; }  // 0-1, how bursty the traffic is
    public bool IsAttack { get; set; }
    
    [VectorType(5)]
    public float[]? Features { get; set; }
}

public class AttackPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsAttack { get; set; }
    
    public float Confidence { get; set; }
}

// Supporting types
public class RequestPattern
{
    public int RequestCount { get; set; }
    public float ErrorRate { get; set; }
    public int UniquePaths { get; set; }
    public float TimeWindow { get; set; }  // Seconds
    public float Burstiness { get; set; }
}

public class RateLimitDecision
{
    public string ClientId { get; set; } = string.Empty;
    public RateLimitAction Action { get; set; }
    public TimeSpan Duration { get; set; }
    public string Reason { get; set; } = string.Empty;
    public float RiskScore { get; set; }
    public int Limit { get; set; }  // Requests per second
    public string SuggestedResponse { get; set; } = string.Empty;
}

public enum RateLimitAction
{
    Allow,
    Monitor,
    Warn,
    StrictLimit,
    Ban
}

public class RiskScore
{
    public string ClientId { get; set; } = string.Empty;
    public float Score { get; set; }
    public float Confidence { get; set; }
    public Dictionary<string, float> Factors { get; set; } = new();
}

public class RateLimitFeedback
{
    public string Decision { get; set; } = string.Empty;
    public RequestPattern Pattern { get; set; } = new();
    public bool WasCorrect { get; set; }
    public bool WasAttack { get; set; }
}

public class ClientPatternHistory
{
    public string ClientId { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public int RequestCount { get; set; }
    public int RecentViolations { get; set; }
    public DateTime LastViolation { get; set; }
}

public interface IRateLimitStore
{
    Task StoreFeedbackAsync(RateLimitFeedback feedback, CancellationToken ct);
}
*/
