using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.MultiAgentOrchestration;

/// <summary>
/// Aggressive context compression for HiveMind swarm mode
/// When 5+ agents communicate, context window fills instantly
/// This service keeps only essential decisions and compresses everything else
/// </summary>
public interface IContextCompressionService
{
    /// <summary>
    /// Compress agent communication history for HiveMind mode
    /// </summary>
    string CompressAgentContext(string context, int targetTokens = 2000);

    /// <summary>
    /// Extract and preserve only the Decision Log from agent communications
    /// </summary>
    DecisionLog ExtractDecisionLog(string context);

    /// <summary>
    /// Compress while maintaining critical reasoning chains
    /// </summary>
    string CompressWithReasoningPreservation(string context);

    /// <summary>
    /// Get compression ratio for monitoring
    /// </summary>
    (int originalTokens, int compressedTokens, double ratio) GetCompressionStats(string original, string compressed);
}

public class ContextCompressionService : IContextCompressionService
{
    private readonly ILogger<ContextCompressionService> _logger;

    // Average tokens per word (conservative estimate for GPT models)
    private const double TokensPerWord = 1.3;

    public ContextCompressionService(ILogger<ContextCompressionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Aggressively compress context for HiveMind mode
    /// Strategy: Keep decisions, summaries, and action items only
    /// </summary>
    public string CompressAgentContext(string context, int targetTokens = 2000)
    {
        if (string.IsNullOrWhiteSpace(context))
            return string.Empty;

        var originalTokenCount = EstimateTokenCount(context);

        // If already within target, return as-is
        if (originalTokenCount <= targetTokens)
            return context;

        _logger.LogInformation(
            "Compressing context from ~{OriginalTokens} to ~{TargetTokens} tokens",
            originalTokenCount, targetTokens);

        // Step 1: Extract Decision Log (highest priority)
        var decisionLog = ExtractDecisionLog(context);

        // Step 2: Remove verbose explanations and reasoning
        var compressed = RemoveVerboseExplanations(context);

        // Step 3: Summarize long discussions
        compressed = SummarizeDiscussions(compressed);

        // Step 4: Remove redundant agent greetings and signatures
        compressed = RemoveRedundantPatterns(compressed);

        // Step 5: Compress code blocks
        compressed = CompressCodeBlocks(compressed);

        // Step 6: Build final context with Decision Log first
        var finalContext = BuildFinalContext(decisionLog, compressed);

        var compressedTokenCount = EstimateTokenCount(finalContext);
        var ratio = (double)originalTokenCount / compressedTokenCount;

        _logger.LogInformation(
            "Context compressed: {OriginalTokens} → {CompressedTokens} tokens ({Ratio:F1}x reduction)",
            originalTokenCount, compressedTokenCount, ratio);

        return finalContext;
    }

    /// <summary>
    /// Extract only decisions from agent communications
    /// </summary>
    public DecisionLog ExtractDecisionLog(string context)
    {
        var decisions = new List<DecisionEntry>();

        // Pattern 1: "We decided to..." / "Decision:..." / "Conclusion:..."
        var decisionPatterns = new[]
        {
            @"(?:Decision|Conclusion|We decided|I propose|Agreed to|Consensus): ?(.+?)(?:\n|$)",
            @"(?:AGENT\s+\w+): ?(.+?[Dd]ecided.+?)(?:\n|$)",
            @"(?:✓|✅|DECISION|ACTION)\u0020?(.+?)(?:\n|$)"
        };

        foreach (var pattern in decisionPatterns)
        {
            var matches = Regex.Matches(context, pattern, RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    decisions.Add(new DecisionEntry
                    {
                        Text = match.Groups[1].Value.Trim(),
                        Timestamp = DateTime.UtcNow,  // In production, parse from context
                        Agent = ExtractAgentName(match.Value),
                        Type = DecisionType.Consensus
                    });
                }
            }
        }

        return new DecisionLog
        {
            Decisions = decisions,
            Count = decisions.Count,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Compress while maintaining reasoning chains
    /// For complex multi-step decisions
    /// </summary>
    public string CompressWithReasoningPreservation(string context)
    {
        // Identify reasoning chains ("Because...", "Therefore...", "This leads to...")
        var reasoningPattern = @"(Because|Therefore|Thus|This leads to|Consequently).+?(?=\n\n|\Z)";
        var reasoningMatches = Regex.Matches(context, reasoningPattern, RegexOptions.Singleline);

        var preservedReasoning = new List<string>();
        foreach (Match match in reasoningMatches)
        {
            if (match.Value.Length < 500) // Only preserve concise reasoning
            {
                preservedReasoning.Add(match.Value.Trim());
            }
        }

        // Compress the rest
        var compressed = CompressAgentContext(context, 1500);

        // Append preserved reasoning
        if (preservedReasoning.Any())
        {
            compressed += "\n\n--- Key Reasoning Chains ---\n";
            compressed += string.Join("\n", preservedReasoning.Take(5));
        }

        return compressed;
    }

    /// <summary>
    /// Calculate compression statistics
    /// </summary>
    public (int originalTokens, int compressedTokens, double ratio) GetCompressionStats(
        string original, string compressed)
    {
        var originalTokens = EstimateTokenCount(original);
        var compressedTokens = EstimateTokenCount(compressed);
        var ratio = compressedTokens > 0 ? (double)originalTokens / compressedTokens : 1.0;

        return (originalTokens, compressedTokens, ratio);
    }

    #region Private Helper Methods

    private int EstimateTokenCount(string text)
    {
        // Rough estimation: words * 1.3 for GPT tokenization
        var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)(wordCount * TokensPerWord);
    }

    private string RemoveVerboseExplanations(string context)
    {
        // Remove "Let me explain...", "To elaborate...", "In detail..."
        var patterns = new[]
        {
            @"Let me explain[.:].*?(?=\n\n|\Z)",
            @"To elaborate[.:].*?(?=\n\n|\Z)",
            @"In detail[,:].*?(?=\n\n|\Z)",
            @"Here is a detailed.*?(?=\n\n|\Z)",
            @"I will now describe.*?(?=\n\n|\Z)"
        };

        foreach (var pattern in patterns)
        {
            context = Regex.Replace(context, pattern, "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        return context;
    }

    private string SummarizeDiscussions(string context)
    {
        // Replace long back-and-forth with summary
        // Pattern: Multiple agent exchanges
        var discussionPattern = @"((?:AGENT\s+\w+[:\-].*?\n){3,})";

        return Regex.Replace(context, discussionPattern, match =>
        {
            var length = match.Value.Length;
            if (length > 1000)
            {
                return $"[Multi-agent discussion: {length} chars summarized - key points above]\n";
            }
            return match.Value;
        }, RegexOptions.Singleline);
    }

    private string RemoveRedundantPatterns(string context)
    {
        // Remove agent signatures and greetings
        var patterns = new[]
        {
            @"^Hello,?\s+(?:team|everyone|all)[.!]?\s*\n?",
            @"Best regards,?\s*\n?.*?$",
            @"Thanks,?\s*\n?.*?$",
            @"___+\n?",
            @"\n{3,}"  // Multiple blank lines
        };

        foreach (var pattern in patterns)
        {
            context = Regex.Replace(context, pattern, "", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        // Normalize whitespace
        context = Regex.Replace(context, @"\n{2,}", "\n\n");

        return context.Trim();
    }

    private string CompressCodeBlocks(string context)
    {
        // Replace long code blocks with [Code: X lines, key logic preserved]
        var codeBlockPattern = @"```[\w]*\n(.*?)```";

        return Regex.Replace(context, codeBlockPattern, match =>
        {
            var code = match.Groups[1].Value;
            var lines = code.Split('\n').Length;

            if (lines > 20)
            {
                // Extract key parts: function signatures, important comments
                var keyParts = code.Split('\n')
                    .Where(l => l.Trim().StartsWith("//") ||
                                l.Trim().StartsWith("///") ||
                                l.Contains("public ") ||
                                l.Contains("function ") ||
                                l.Contains("def ") ||
                                l.Contains("class "))
                    .Take(5);

                return $"```\n[Code block: {lines} lines - Key parts:]\n{string.Join("\n", keyParts)}\n[... see full code in workspace ...]\n```";
            }

            return match.Value;
        }, RegexOptions.Singleline);
    }

    private string ExtractAgentName(string text)
    {
        var match = Regex.Match(text, @"AGENT\s+(\w+)");
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    private string BuildFinalContext(DecisionLog decisionLog, string compressed)
    {
        var sb = new System.Text.StringBuilder();

        // Priority 1: Decision Log
        sb.AppendLine("=== DECISION LOG ===");
        foreach (var decision in decisionLog.Decisions.Take(10))  // Keep last 10
        {
            sb.AppendLine($"[{decision.Agent}] {decision.Text}");
        }
        sb.AppendLine();

        // Priority 2: Compressed context
        sb.AppendLine("=== CONTEXT ===");
        sb.AppendLine(compressed);

        return sb.ToString();
    }

    #endregion
}

/// <summary>
/// A single decision entry
/// </summary>
public class DecisionEntry
{
    public string Text { get; set; } = string.Empty;
    public string Agent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DecisionType Type { get; set; }
}

/// <summary>
/// Collection of decisions from agent swarm
/// </summary>
public class DecisionLog
{
    public List<DecisionEntry> Decisions { get; set; } = new();
    public int Count { get; set; }
    public DateTime LastUpdated { get; set; }
}

public enum DecisionType
{
    Consensus,
    Proposal,
    Action,
    Rejection,
    Clarification
}
