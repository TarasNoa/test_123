namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Recovery;

/// <summary>
/// Switches to a fallback/reserve model when the primary model is unavailable.
/// </summary>
public class FallbackModelRecoveryStrategy : IRecoveryStrategy
{
    private readonly string? _fallbackModel;

    public FallbackModelRecoveryStrategy(string? fallbackModel = null)
    {
        _fallbackModel = fallbackModel;
    }

    public string GetStrategyName() => "FallbackModel";

    public bool CanRecover(Exception exception, RecoveryContext context)
    {
        // Recover from provider errors or timeouts
        return IsProviderError(exception) && !string.IsNullOrEmpty(_fallbackModel);
    }

    public Task<RecoveryResult> RecoverAsync(RecoveryContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Switch to fallback model
        context.Metadata["OriginalModel"] = context.Metadata.GetValueOrDefault("CurrentModel", "unknown");
        context.Metadata["CurrentModel"] = _fallbackModel;
        context.Metadata["FallbackTriggered"] = true;

        var result = new RecoveryResult
        {
            Success = true,
            StrategyUsed = GetStrategyName(),
            ContextAfterRecovery = context,
            Reason = $"Switched to fallback model: {_fallbackModel}",
            Duration = DateTime.UtcNow - startTime
        };

        return Task.FromResult(result);
    }

    private bool IsProviderError(Exception exception)
    {
        if (exception == null) return false;
        
        var message = exception.Message.ToLowerInvariant();
        return message.Contains("timeout") ||
               message.Contains("unavailable") ||
               message.Contains("rate limit") ||
               message.Contains("service unavailable") ||
               message.Contains("503") ||
               message.Contains("429");
    }
}
