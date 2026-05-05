using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Recovery;

/// <summary>
/// Orchestrates a cascade of recovery strategies for LLM pipeline errors.
/// Attempts strategies in order until one succeeds or all are exhausted.
/// Includes circuit breaker pattern to skip strategies that consistently fail,
/// and caching to avoid redundant recovery attempts for identical contexts.
/// INTERNAL: This service is for internal use only and will not be included in public builds.
/// </summary>
public class RecoveryCascadeService
{
    private const int MaxCacheEntries = 1000;
    private static readonly TimeSpan FailureDecayInterval = TimeSpan.FromMinutes(15);

    private readonly ILogger<RecoveryCascadeService> _logger;
    private readonly RecoveryOptions _options;
    private readonly List<IRecoveryStrategy> _strategies;
    private readonly ConcurrentDictionary<string, int> _strategyFailureCounts = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTime> _strategyFailureLastSeen = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (RecoveryResult Result, DateTime AddedAt)> _recoveryCache = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> _cacheInsertionOrder = new();

    public RecoveryCascadeService(
        ILogger<RecoveryCascadeService> logger,
        RecoveryOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new RecoveryOptions();

        _strategies = new List<IRecoveryStrategy>
        {
            new ContextMicroCompressionRecoveryStrategy(),
            new ContextCollapseRecoveryStrategy(),
            new TokenEscalationRecoveryStrategy(),
            new FallbackModelRecoveryStrategy(_options.FallbackModel)
        };
    }

    /// <summary>
    /// Attempts to recover from an error using the cascade of strategies.
    /// Uses caching to avoid redundant recovery attempts for identical contexts.
    /// </summary>
    public async Task<RecoveryCascadeResult> AttemptRecoveryAsync(
        Exception exception,
        RecoveryContext context,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var attempts = new List<RecoveryAttempt>();
        var maxAttempts = _options.MaxRecoveryAttempts;

        // Check cache for identical context (based on exception type and prompt content hash)
        var cacheKey = BuildCacheKey(exception, context);
        if (_recoveryCache.TryGetValue(cacheKey, out var cached))
        {
            var cachedResult = cached.Result;
            _logger.LogInformation("Using cached recovery result for exception: {ExceptionType}", exception.GetType().Name);
            return new RecoveryCascadeResult
            {
                Success = cachedResult.Success,
                StrategyUsed = cachedResult.StrategyUsed ?? "cached",
                ContextAfterRecovery = cachedResult.ContextAfterRecovery ?? context,
                Attempts = new List<RecoveryAttempt>
                {
                    new RecoveryAttempt
                    {
                        StrategyName = "cache",
                        Success = cachedResult.Success,
                        Reason = "Retrieved from cache",
                        Duration = TimeSpan.Zero
                    }
                },
                TotalDuration = DateTime.UtcNow - startTime
            };
        }

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            context.RecoveryAttempt = attempt;

            foreach (var strategy in _strategies)
            {
                // Decay failure counter if window elapsed.
                MaybeDecayFailureCounter(strategy.GetStrategyName());

                // Skip strategies that have failed too many times (circuit breaker)
                if (_strategyFailureCounts.GetOrAdd(strategy.GetStrategyName(), 0) >= _options.CircuitBreakerThreshold)
                {
                    _logger.LogDebug("Skipping strategy {StrategyName} due to circuit breaker (failures: {Failures})",
                        strategy.GetStrategyName(),
                        _strategyFailureCounts[strategy.GetStrategyName()]);
                    continue;
                }

                if (!strategy.CanRecover(exception, context))
                {
                    continue;
                }

                try
                {
                    _logger.LogInformation(
                        "Attempting recovery with strategy: {StrategyName} (attempt {Attempt}/{MaxAttempts})",
                        strategy.GetStrategyName(),
                        attempt + 1,
                        maxAttempts);

                    var result = await strategy.RecoverAsync(context, cancellationToken);

                    attempts.Add(new RecoveryAttempt
                    {
                        StrategyName = strategy.GetStrategyName(),
                        Success = result.Success,
                        Reason = result.Reason,
                        Duration = result.Duration
                    });

                    if (result.Success)
                    {
                        // Reset failure count on success
                        _strategyFailureCounts[strategy.GetStrategyName()] = 0;

                        // Cache the successful result (with bounded size)
                        AddToCache(cacheKey, result);

                        _logger.LogInformation(
                            "Recovery succeeded with strategy: {StrategyName}",
                            strategy.GetStrategyName());

                        return new RecoveryCascadeResult
                        {
                            Success = true,
                            StrategyUsed = strategy.GetStrategyName(),
                            ContextAfterRecovery = result.ContextAfterRecovery,
                            Attempts = attempts,
                            TotalDuration = DateTime.UtcNow - startTime
                        };
                    }
                    else
                    {
                        // Increment failure count atomically
                        _strategyFailureCounts.AddOrUpdate(strategy.GetStrategyName(), 1, (_, v) => v + 1);
                        _strategyFailureLastSeen[strategy.GetStrategyName()] = DateTime.UtcNow;

                        _logger.LogWarning(
                            "Recovery failed with strategy: {StrategyName}, reason: {Reason}",
                            strategy.GetStrategyName(),
                            result.Reason);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Exception during recovery with strategy: {StrategyName}",
                        strategy.GetStrategyName());

                    _strategyFailureCounts.AddOrUpdate(strategy.GetStrategyName(), 1, (_, v) => v + 1);
                    _strategyFailureLastSeen[strategy.GetStrategyName()] = DateTime.UtcNow;

                    attempts.Add(new RecoveryAttempt
                    {
                        StrategyName = strategy.GetStrategyName(),
                        Success = false,
                        Reason = $"Exception: {ex.Message}",
                        Duration = TimeSpan.Zero
                    });
                }
            }
        }

        // All strategies failed - cache the failure to avoid repeating
        AddToCache(cacheKey, new RecoveryResult
        {
            Success = false,
            StrategyUsed = "none",
            Reason = "All strategies exhausted",
            ContextAfterRecovery = context
        });

        _logger.LogError("All recovery strategies exhausted after {Attempts} attempts", attempts.Count);

        return new RecoveryCascadeResult
        {
            Success = false,
            StrategyUsed = "none",
            ContextAfterRecovery = context,
            Attempts = attempts,
            TotalDuration = DateTime.UtcNow - startTime
        };
    }

    /// <summary>
    /// Builds a cache key from exception type AND prompt content hash so
    /// distinct prompts with similar token counts don't collide.
    /// </summary>
    private static string BuildCacheKey(Exception exception, RecoveryContext context)
    {
        var sb = new StringBuilder(256);
        sb.Append(exception.GetType().Name).Append('|');
        sb.Append("Tokens:").Append(context.CurrentTokenCount).Append('|');
        sb.Append("Messages:").Append(context.MessageHistory.Count).Append('|');
        sb.Append("Attempt:").Append(context.RecoveryAttempt).Append('|');
        // Content hash: take last prompt + recent messages, hash with SHA256.
        var prompt = context.CurrentPrompt ?? string.Empty;
        var recent = context.MessageHistory.Count > 0
            ? string.Join("\n", context.MessageHistory.TakeLast(3))
            : string.Empty;
        var bytes = Encoding.UTF8.GetBytes(prompt + "\u0001" + recent);
        var hash = SHA256.HashData(bytes);
        sb.Append("Hash:").Append(Convert.ToHexString(hash, 0, 8));
        return sb.ToString();
    }

    private void MaybeDecayFailureCounter(string strategyName)
    {
        if (!_strategyFailureLastSeen.TryGetValue(strategyName, out var last))
            return;
        if (DateTime.UtcNow - last < FailureDecayInterval)
            return;
        // After the decay window, halve the counter (slow recovery from past failures).
        _strategyFailureCounts.AddOrUpdate(strategyName, 0, (_, v) => Math.Max(0, v / 2));
        _strategyFailureLastSeen[strategyName] = DateTime.UtcNow;
    }

    private void AddToCache(string key, RecoveryResult result)
    {
        _recoveryCache[key] = (result, DateTime.UtcNow);
        _cacheInsertionOrder.Enqueue(key);

        if (_recoveryCache.Count > MaxCacheEntries)
        {
            if (_cacheInsertionOrder.TryDequeue(out var oldestKey))
            {
                _recoveryCache.TryRemove(oldestKey, out _);
            }
        }
    }

    /// <summary>
    /// Clears the recovery cache. Useful for testing or when context changes significantly.
    /// </summary>
    public void ClearCache()
    {
        _recoveryCache.Clear();
        while (_cacheInsertionOrder.TryDequeue(out _)) { }
        _logger.LogInformation("Recovery cache cleared");
    }

    /// <summary>
    /// Resets circuit breaker failure counts. Useful for testing or after a recovery period.
    /// </summary>
    public void ResetCircuitBreakers()
    {
        _strategyFailureCounts.Clear();
        _logger.LogInformation("Circuit breaker failure counts reset");
    }
}
/// <summary>
/// Configuration options for recovery behavior.
/// </summary>
public class RecoveryOptions
{
    /// <summary>
    /// Maximum number of recovery attempts before giving up.
    /// </summary>
    public int MaxRecoveryAttempts { get; set; } = 3;

    /// <summary>
    /// Number of consecutive failures before a strategy is skipped (circuit breaker).
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 3;

    /// <summary>
    /// Fallback model to use when primary model is unavailable.
    /// </summary>
    public string? FallbackModel { get; set; }

    /// <summary>
    /// Maximum total time allowed for recovery attempts.
    /// </summary>
    public TimeSpan MaxRecoveryDuration { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Result of a recovery cascade attempt.
/// </summary>
public class RecoveryCascadeResult
{
    public bool Success { get; set; }
    public string StrategyUsed { get; set; } = string.Empty;
    public RecoveryContext ContextAfterRecovery { get; set; } = new();
    public List<RecoveryAttempt> Attempts { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
}

/// <summary>
/// Individual recovery attempt record.
/// </summary>
public class RecoveryAttempt
{
    public string StrategyName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Reason { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}
