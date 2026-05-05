namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Recovery;

/// <summary>
/// Inserts a continuation hint when model exhausts output limit mid-task.
/// Subtly instructs model to continue without apologies or repetition.
/// </summary>
public class TokenEscalationRecoveryStrategy : IRecoveryStrategy
{
    private const int MaxContinuationAttempts = 3;

    public string GetStrategyName() => "TokenEscalation";

    public bool CanRecover(Exception exception, RecoveryContext context)
    {
        // Only recover from token limit errors
        return IsTokenLimitError(exception) && context.RecoveryAttempt < MaxContinuationAttempts;
    }

    public Task<RecoveryResult> RecoverAsync(RecoveryContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Add continuation hint to prompt
        var continuationHint = "\n\nContinue immediately, without apologies or repetition. Complete the task from where you left off.";
        
        context.CurrentPrompt += continuationHint;
        context.Metadata["ContinuationAdded"] = true;
        context.Metadata["ContinuationAttempt"] = context.RecoveryAttempt + 1;

        var result = new RecoveryResult
        {
            Success = true,
            StrategyUsed = GetStrategyName(),
            ContextAfterRecovery = context,
            Reason = $"Added continuation hint (attempt {context.RecoveryAttempt + 1}/{MaxContinuationAttempts})",
            Duration = DateTime.UtcNow - startTime
        };

        return Task.FromResult(result);
    }

    private bool IsTokenLimitError(Exception exception)
    {
        if (exception == null) return false;
        
        var message = exception.Message.ToLowerInvariant();
        return message.Contains("token") && 
               (message.Contains("limit") || message.Contains("exceed") || message.Contains("maximum") ||
                message.Contains("truncat") || message.Contains("cut off"));
    }
}
