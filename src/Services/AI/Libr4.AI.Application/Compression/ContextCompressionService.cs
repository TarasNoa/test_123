using Microsoft.Extensions.Logging;

namespace Libr4.AI.Application.Compression;

/// <summary>
/// Service for compressing context before sending to LLM.
/// Implements multiple strategies: semantic summarization, importance ranking, 
/// sliding window, and hierarchical summarization.
/// </summary>
public interface IContextCompressionService
{
    /// <summary>
    /// Compress a list of context items to fit within token budget.
    /// </summary>
    Task<CompressionResult> CompressAsync(
        IReadOnlyList<ContextItem> items,
        CompressionOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Summarize a single large context item.
    /// </summary>
    Task<ContextItem> SummarizeAsync(
        ContextItem item,
        int targetTokens,
        CancellationToken ct = default);

    /// <summary>
    /// Rank items by importance for prioritization.
    /// </summary>
    Task<IReadOnlyList<RankedContextItem>> RankByImportanceAsync(
        IReadOnlyList<ContextItem> items,
        string? query = null,
        CancellationToken ct = default);
}

/// <summary>
/// Main implementation using hybrid approach.
/// </summary>
public sealed class ContextCompressionService : IContextCompressionService
{
    private readonly ILogger<ContextCompressionService> _logger;

    // Token estimation: ~4 chars per token (conservative)
    private const double CharsPerToken = 4.0;

    public ContextCompressionService(
        ILogger<ContextCompressionService> logger)
    {
        _logger = logger;
    }

    public async Task<CompressionResult> CompressAsync(
        IReadOnlyList<ContextItem> items,
        CompressionOptions options,
        CancellationToken ct = default)
    {
        var targetTokens = options.TargetTokens;
        var currentTokens = items.Sum(EstimateTokens);

        _logger.LogDebug(
            "Compressing {Count} items from {CurrentTokens} to {TargetTokens} tokens",
            items.Count, currentTokens, targetTokens);

        // If already within budget, no compression needed
        if (currentTokens <= targetTokens)
        {
            return new CompressionResult
            {
                Items = items,
                OriginalTokenCount = currentTokens,
                FinalTokenCount = currentTokens,
                CompressionRatio = 1.0,
                Strategy = CompressionStrategy.None
            };
        }

        // Try different strategies in order of sophistication
        IReadOnlyList<ContextItem> result;
        CompressionStrategy strategyUsed;

        if (options.Strategies.Contains(CompressionStrategy.SemanticRanking))
        {
            result = await CompressBySemanticRankingAsync(items, targetTokens, options.Query, ct);
            strategyUsed = CompressionStrategy.SemanticRanking;
        }
        else if (options.Strategies.Contains(CompressionStrategy.HierarchicalSummarization))
        {
            result = await CompressHierarchicallyAsync(items, targetTokens, ct);
            strategyUsed = CompressionStrategy.HierarchicalSummarization;
        }
        else if (options.Strategies.Contains(CompressionStrategy.SlidingWindow))
        {
            result = CompressBySlidingWindow(items, targetTokens);
            strategyUsed = CompressionStrategy.SlidingWindow;
        }
        else
        {
            // Fallback: simple truncation with importance weighting
            result = CompressBySimpleTruncation(items, targetTokens);
            strategyUsed = CompressionStrategy.Truncation;
        }

        var finalTokens = result.Sum(EstimateTokens);
        var ratio = (double)finalTokens / currentTokens;

        _logger.LogInformation(
            "Compressed context: {OriginalTokens} -> {FinalTokens} tokens ({Ratio:P}) using {Strategy}",
            currentTokens, finalTokens, ratio, strategyUsed);

        return new CompressionResult
        {
            Items = result,
            OriginalTokenCount = currentTokens,
            FinalTokenCount = finalTokens,
            CompressionRatio = ratio,
            Strategy = strategyUsed,
            DroppedItems = items.Count - result.Count
        };
    }

    public async Task<ContextItem> SummarizeAsync(
        ContextItem item,
        int targetTokens,
        CancellationToken ct = default)
    {
        var currentTokens = EstimateTokens(item);
        if (currentTokens <= targetTokens)
        {
            return item;
        }

        var summary = await GenerateSummaryAsync(item.Content, targetTokens, ct);
        
        return new ContextItem
        {
            Id = item.Id,
            Type = item.Type,
            Source = item.Source,
            Content = summary,
            OriginalLength = item.Content.Length,
            Metadata = new Dictionary<string, object>(item.Metadata)
            {
                ["summarized"] = true,
                ["original_tokens"] = currentTokens,
                ["summary_tokens"] = EstimateTokens(summary)
            }
        };
    }

    public async Task<IReadOnlyList<RankedContextItem>> RankByImportanceAsync(
        IReadOnlyList<ContextItem> items,
        string? query = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(query))
        {
            // Rank by inherent importance signals
            return items.Select(item => new RankedContextItem
            {
                Item = item,
                ImportanceScore = CalculateImportanceScore(item),
                Reason = "Inherent importance"
            }).OrderByDescending(r => r.ImportanceScore).ToList();
        }

        // Semantic ranking against query (temporarily disabled)
        // var queryEmbedding = await _embeddings.GenerateEmbeddingAsync(query, cancellationToken: ct);
        var queryEmbedding = new float[1536];  // placeholder
        
        var ranked = new List<RankedContextItem>();
        foreach (var item in items)
        {
            // var itemEmbedding = await _embeddings.GenerateEmbeddingAsync(
            //     item.Content[..Math.Min(item.Content.Length, 1000)], // Limit for performance
            //     cancellationToken: ct);
            var itemEmbedding = new float[1536];  // placeholder
            
            var similarity = CosineSimilarity(queryEmbedding, itemEmbedding);
            
            // Combine semantic similarity with inherent importance
            var importance = CalculateImportanceScore(item);
            var combinedScore = (similarity * 0.7) + (importance * 0.3);

            ranked.Add(new RankedContextItem
            {
                Item = item,
                ImportanceScore = combinedScore,
                SemanticSimilarity = similarity,
                InherentImportance = importance,
                Reason = $"Similarity: {similarity:F2}, Importance: {importance:F2}"
            });
        }

        return ranked.OrderByDescending(r => r.ImportanceScore).ToList();
    }

    private async Task<IReadOnlyList<ContextItem>> CompressBySemanticRankingAsync(
        IReadOnlyList<ContextItem> items,
        int targetTokens,
        string? query,
        CancellationToken ct)
    {
        var ranked = await RankByImportanceAsync(items, query, ct);
        
        var selected = new List<ContextItem>();
        var currentTokens = 0;

        foreach (var rankedItem in ranked)
        {
            var itemTokens = EstimateTokens(rankedItem.Item);
            
            if (currentTokens + itemTokens <= targetTokens)
            {
                selected.Add(rankedItem.Item);
                currentTokens += itemTokens;
            }
            else
            {
                // Try to summarize if it's a large item
                if (itemTokens > 200 && currentTokens + 100 <= targetTokens)
                {
                    var summarized = await SummarizeAsync(rankedItem.Item, 100, ct);
                    selected.Add(summarized);
                    currentTokens += 100;
                }
            }

            if (currentTokens >= targetTokens * 0.95) // Leave 5% buffer
                break;
        }

        return selected;
    }

    private async Task<IReadOnlyList<ContextItem>> CompressHierarchicallyAsync(
        IReadOnlyList<ContextItem> items,
        int targetTokens,
        CancellationToken ct)
    {
        // Group items by type/source for hierarchical summarization
        var groups = items.GroupBy(i => i.Type).ToList();
        var summaries = new List<ContextItem>();

        foreach (var group in groups)
        {
            var groupItems = group.ToList();
            var groupTokens = groupItems.Sum(EstimateTokens);
            var groupBudget = (int)(targetTokens * ((double)groupTokens / items.Sum(EstimateTokens)));

            if (groupTokens <= groupBudget)
            {
                // Group fits within budget
                summaries.AddRange(groupItems);
            }
            else if (groupItems.Count == 1)
            {
                // Single large item - summarize it
                var summarized = await SummarizeAsync(groupItems[0], groupBudget, ct);
                summaries.Add(summarized);
            }
            else
            {
                // Multiple items - create group summary
                var combinedContent = string.Join("\n\n", groupItems.Select(i => 
                    $"[{i.Source}]: {i.Content}"));
                
                var groupSummary = await GenerateSummaryAsync(combinedContent, groupBudget, ct);
                
                summaries.Add(new ContextItem
                {
                    Id = $"group_{group.Key}",
                    Type = group.Key,
                    Source = $"Summary of {groupItems.Count} items",
                    Content = groupSummary,
                    Metadata = new Dictionary<string, object>
                    {
                        ["grouped_items"] = groupItems.Select(i => i.Id).ToList(),
                        ["original_count"] = groupItems.Count
                    }
                });
            }
        }

        return summaries;
    }

    private IReadOnlyList<ContextItem> CompressBySlidingWindow(
        IReadOnlyList<ContextItem> items,
        int targetTokens)
    {
        // Keep most recent items that fit in budget
        var selected = new List<ContextItem>();
        var currentTokens = 0;

        // Process in reverse (newest first)
        foreach (var item in items.Reverse())
        {
            var itemTokens = EstimateTokens(item);
            
            if (currentTokens + itemTokens <= targetTokens)
            {
                selected.Insert(0, item); // Insert at beginning to maintain order
                currentTokens += itemTokens;
            }
            else
            {
                // Truncate if it's the last item we can fit
                var remaining = targetTokens - currentTokens;
                if (remaining > 50) // Minimum meaningful truncation
                {
                    var truncated = Truncate(item, remaining);
                    selected.Insert(0, truncated);
                }
                break;
            }
        }

        return selected;
    }

    private IReadOnlyList<ContextItem> CompressBySimpleTruncation(
        IReadOnlyList<ContextItem> items,
        int targetTokens)
    {
        var selected = new List<ContextItem>();
        var currentTokens = 0;

        foreach (var item in items.OrderByDescending(CalculateImportanceScore))
        {
            var itemTokens = EstimateTokens(item);
            
            if (currentTokens + itemTokens <= targetTokens)
            {
                selected.Add(item);
                currentTokens += itemTokens;
            }
            else
            {
                // Try partial inclusion
                var remaining = targetTokens - currentTokens;
                if (remaining > 100)
                {
                    selected.Add(Truncate(item, remaining));
                }
                break;
            }
        }

        return selected;
    }

    private async Task<string> GenerateSummaryAsync(
        string content, 
        int targetTokens, 
        CancellationToken ct)
    {
        var maxChars = (int)(targetTokens * CharsPerToken);
        
        var prompt = $@"Summarize the following content concisely while preserving key information:

{content[..Math.Min(content.Length, 10000)]}

Provide a summary in {(maxChars / 4)} words or less:";

        try
        {
            // var response = await _llmProvider.CompleteAsync(new CompletionRequest(
            //     Model: "gpt-4o-mini", // Use fast, cheap model for summarization
            //     Messages: new List<ChatMessage>
            //     {
            //         new("system", "You are a summarization assistant. Create concise summaries that preserve essential information."),
            //         new("user", prompt)
            //     },
            //     MaxTokens: targetTokens / 2,
            //     Temperature: 0.3f
            // ), ct);
            // Temporarily disabled - return placeholder
            var compressedContent = prompt.Substring(0, Math.Min(prompt.Length, targetTokens / 2));
            return compressedContent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM summarization failed, using truncation");
        }

        // Fallback: simple truncation with ellipsis
        if (content.Length > maxChars)
        {
            return content.Substring(0, maxChars - 3) + "...";
        }

        return content;
    }

    private double CalculateImportanceScore(ContextItem item)
    {
        var score = 1.0;

        // Type-based weighting
        score *= item.Type.ToLower() switch
        {
            "system" => 2.0,
            "user_query" => 1.8,
            "error" => 1.6,
            "file_content" => 1.4,
            "search_result" => 1.3,
            "memory" => 1.2,
            "chat_history" => 1.0,
            _ => 1.0
        };

        // Recency bonus (if timestamp available)
        if (item.Metadata.TryGetValue("timestamp", out var tsObj) && tsObj is DateTime ts)
        {
            var age = DateTime.UtcNow - ts;
            if (age < TimeSpan.FromMinutes(5)) score *= 1.5;
            else if (age < TimeSpan.FromMinutes(30)) score *= 1.3;
            else if (age < TimeSpan.FromHours(1)) score *= 1.1;
        }

        // Length penalty (very long items are less focused)
        var length = item.Content.Length;
        if (length > 5000) score *= 0.7;
        else if (length > 2000) score *= 0.85;

        // Explicit importance flag
        if (item.Metadata.TryGetValue("importance", out var impObj) && 
            impObj is double importance)
        {
            score *= importance;
        }

        return score;
    }

    private static int EstimateTokens(ContextItem item)
    {
        return (int)(item.Content.Length / CharsPerToken) + 10; // +10 for overhead
    }

    private static int EstimateTokens(string text)
    {
        return (int)(text.Length / CharsPerToken) + 10;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private static ContextItem Truncate(ContextItem item, int targetTokens)
    {
        var maxChars = (int)(targetTokens * CharsPerToken) - 10;
        var truncated = item.Content.Length > maxChars 
            ? item.Content.Substring(0, maxChars - 3) + "..."
            : item.Content;

        return new ContextItem
        {
            Id = item.Id,
            Type = item.Type,
            Source = item.Source,
            Content = truncated,
            OriginalLength = item.Content.Length,
            Metadata = new Dictionary<string, object>(item.Metadata)
            {
                ["truncated"] = true,
                ["original_length"] = item.Content.Length
            }
        };
    }
}

// Data models

public enum CompressionStrategy
{
    None,
    Truncation,
    SlidingWindow,
    SemanticRanking,
    HierarchicalSummarization
}

public sealed class CompressionOptions
{
    public int TargetTokens { get; init; } = 4000;
    public string? Query { get; init; } // For semantic ranking relevance
    public IReadOnlyList<CompressionStrategy> Strategies { get; init; } = 
        new[] { CompressionStrategy.SemanticRanking, CompressionStrategy.HierarchicalSummarization, CompressionStrategy.SlidingWindow };
    public bool AllowSummarization { get; init; } = true;
}

public sealed class CompressionResult
{
    public IReadOnlyList<ContextItem> Items { get; init; } = new List<ContextItem>();
    public int OriginalTokenCount { get; init; }
    public int FinalTokenCount { get; init; }
    public double CompressionRatio { get; init; }
    public CompressionStrategy Strategy { get; init; }
    public int DroppedItems { get; init; }
}

public sealed class ContextItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Type { get; init; } = "unknown"; // system, user_query, file_content, etc.
    public string Source { get; init; } = "";
    public string Content { get; init; } = "";
    public int? OriginalLength { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public sealed class RankedContextItem
{
    public ContextItem Item { get; init; } = null!;
    public double ImportanceScore { get; init; }
    public double? SemanticSimilarity { get; init; }
    public double? InherentImportance { get; init; }
    public string Reason { get; init; } = "";
}
