namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Recovery;

/// <summary>
/// Removes least significant messages from context to free up tokens.
/// Scoring considers: recency + stage importance.
/// </summary>
public class ContextMicroCompressionRecoveryStrategy : IRecoveryStrategy
{
    public string GetStrategyName() => "ContextMicroCompression";

    public bool CanRecover(Exception exception, RecoveryContext context)
    {
        // Only recover from token limit errors
        return IsTokenLimitError(exception) && context.MessageHistory.Count > 1;
    }

    public Task<RecoveryResult> RecoverAsync(RecoveryContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Score messages: higher score = more important (keep)
        var scoredMessages = context.MessageHistory
            .Select((msg, index) => new
            {
                Message = msg,
                Index = index,
                Score = CalculateScore(msg, index, context.MessageHistory.Count)
            })
            .OrderBy(x => x.Score)
            .ToList();

        // Remove bottom 20% of messages (least significant)
        var messagesToRemove = Math.Max(1, scoredMessages.Count / 5);
        for (var i = 0; i < messagesToRemove; i++)
        {
            var toRemove = scoredMessages[i];
            context.MessageHistory.RemoveAt(toRemove.Index);
        }

        // Rebuild prompt from remaining messages
        context.CurrentPrompt = string.Join("\n", context.MessageHistory);
        context.CurrentTokenCount = EstimateTokenCount(context.CurrentPrompt);

        var result = new RecoveryResult
        {
            Success = true,
            StrategyUsed = GetStrategyName(),
            ContextAfterRecovery = context,
            Reason = $"Removed {messagesToRemove} least significant messages",
            Duration = DateTime.UtcNow - startTime
        };

        return Task.FromResult(result);
    }

    private bool IsTokenLimitError(Exception exception)
    {
        if (exception == null) return false;
        
        var message = exception.Message.ToLowerInvariant();
        return message.Contains("token") && 
               (message.Contains("limit") || message.Contains("exceed") || message.Contains("maximum"));
    }

    private double CalculateScore(string message, int index, int totalMessages)
    {
        // Base score on recency (more recent = higher score)
        var recencyScore = (double)index / totalMessages;

        // Boost score for important keywords
        var importanceScore = 0.0;
        var importantKeywords = new[] { "error", "exception", "fail", "critical", "security", "auth", "secret" };
        var lowerMessage = message.ToLowerInvariant();
        
        foreach (var keyword in importantKeywords)
        {
            if (lowerMessage.Contains(keyword))
            {
                importanceScore += 0.2;
            }
        }

        return recencyScore + importanceScore;
    }

    private int EstimateTokenCount(string text)
    {
        // Rough estimation: ~4 characters per token
        return text.Length / 4;
    }
}
