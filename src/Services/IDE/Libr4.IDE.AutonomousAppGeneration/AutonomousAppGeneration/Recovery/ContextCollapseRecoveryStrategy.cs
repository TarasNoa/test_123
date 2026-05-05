namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Recovery;

/// <summary>
/// Collapses dialog history into brief summaries (10 messages -> 1 summary).
/// </summary>
public class ContextCollapseRecoveryStrategy : IRecoveryStrategy
{
    public string GetStrategyName() => "ContextCollapse";

    public bool CanRecover(Exception exception, RecoveryContext context)
    {
        // Recover from token limit errors when we have enough messages to collapse
        return IsTokenLimitError(exception) && context.MessageHistory.Count >= 10;
    }

    public Task<RecoveryResult> RecoverAsync(RecoveryContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Group messages into batches of 10
        var batchSize = 10;
        var collapsedMessages = new List<string>();

        for (var i = 0; i < context.MessageHistory.Count; i += batchSize)
        {
            var batch = context.MessageHistory.Skip(i).Take(batchSize).ToList();
            var summary = CollapseBatch(batch, i);
            collapsedMessages.Add(summary);
        }

        // Replace message history with collapsed summaries
        context.MessageHistory = collapsedMessages;
        context.CurrentPrompt = string.Join("\n", context.MessageHistory);
        context.CurrentTokenCount = EstimateTokenCount(context.CurrentPrompt);

        var result = new RecoveryResult
        {
            Success = true,
            StrategyUsed = GetStrategyName(),
            ContextAfterRecovery = context,
            Reason = $"Collapsed {context.MessageHistory.Count} messages into {collapsedMessages.Count} summaries",
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

    private string CollapseBatch(List<string> messages, int startIndex)
    {
        // Extract key information from batch
        var keyPoints = new List<string>();
        
        foreach (var msg in messages)
        {
            // Look for important patterns
            if (msg.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("exception", StringComparison.OrdinalIgnoreCase))
            {
                keyPoints.Add($"[MSG_{startIndex}] Error/failure detected");
            }
            
            if (msg.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("security", StringComparison.OrdinalIgnoreCase))
            {
                keyPoints.Add($"[MSG_{startIndex}] Auth/security context");
            }

            if (msg.Contains("complete", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("done", StringComparison.OrdinalIgnoreCase))
            {
                keyPoints.Add($"[MSG_{startIndex}] Task completed");
            }
        }

        if (keyPoints.Count == 0)
        {
            return $"[MSG_{startIndex}-{startIndex + messages.Count - 1}] {messages.Count} messages processed (no key events)";
        }

        return $"[MSG_{startIndex}-{startIndex + messages.Count - 1}] {string.Join("; ", keyPoints)}";
    }

    private int EstimateTokenCount(string text)
    {
        return text.Length / 4;
    }
}
