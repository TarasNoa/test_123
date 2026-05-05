using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.PromptOptimization;

/// <summary>
/// Prompt optimization: whitespace normalization, redundancy removal,
/// approximate token counting, and structured variant generation.
/// </summary>
public class PromptOptimizationService : IPromptOptimizationService
{
    private readonly ILogger<PromptOptimizationService> _logger;

    private static readonly Regex MultiSpace = new(@"[^\S\n]{2,}", RegexOptions.Compiled);
    private static readonly Regex MultiNewline = new(@"\n{3,}", RegexOptions.Compiled);
    private static readonly Regex RedundantPhrases = new(
        @"\b(please|kindly|could you|would you mind|as an ai language model|certainly!|of course!|sure,)\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public PromptOptimizationService(ILogger<PromptOptimizationService> logger)
    {
        _logger = logger;
    }

    public Task<string> OptimizeAsync(string prompt, string model = "default", CancellationToken ct = default)
    {
        _logger.LogInformation("Optimizing prompt for model: {Model} ({Tokens} tokens)", model, CountTokens(prompt));

        var optimized = prompt;
        optimized = MultiSpace.Replace(optimized, " ");
        optimized = MultiNewline.Replace(optimized, "\n\n");
        optimized = RedundantPhrases.Replace(optimized, string.Empty);
        optimized = optimized.Trim();

        var modelLower = model.ToLowerInvariant();
        if (modelLower.Contains("gpt-4") || modelLower.Contains("claude"))
        {
            if (!optimized.EndsWith(".", StringComparison.Ordinal) &&
                !optimized.EndsWith("?", StringComparison.Ordinal) &&
                !optimized.EndsWith("!", StringComparison.Ordinal))
                optimized += ".";
        }

        _logger.LogDebug("Prompt optimized: {Before} -> {After} tokens",
            CountTokens(prompt), CountTokens(optimized));

        return Task.FromResult(optimized);
    }

    public Task<string[]> SuggestVariantsAsync(string basePrompt, int count = 3, CancellationToken ct = default)
    {
        _logger.LogInformation("Suggesting {Count} variants", count);
        count = Math.Clamp(count, 1, 5);

        var base_ = basePrompt.Trim();
        var variants = new List<string> { base_ };

        if (count >= 2)
            variants.Add($"Step by step: {base_}");
        if (count >= 3)
            variants.Add($"{base_} Provide a concise, structured response.");
        if (count >= 4)
            variants.Add($"As an expert, {char.ToLowerInvariant(base_[0])}{base_[1..]} Use precise technical language.");
        if (count >= 5)
            variants.Add($"Think through this carefully: {base_} Output only the essential information.");

        return Task.FromResult(variants.Take(count).ToArray());
    }

    public Task<PromptAnalysis> AnalyzeAsync(string prompt, CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing prompt ({Length} chars)", prompt.Length);

        var tokenCount = CountTokens(prompt);
        var sentences = prompt.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var wordCount = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var hasQuestion = prompt.Contains('?');
        var hasInstructions = prompt.Contains(':') || prompt.StartsWith("You are", StringComparison.OrdinalIgnoreCase);
        var complexity = Math.Min(1.0, tokenCount / 500.0 + (hasInstructions ? 0.2 : 0) + (sentences > 3 ? 0.1 : 0));
        var isWellFormed = wordCount >= 3 && tokenCount <= 4096 && !string.IsNullOrWhiteSpace(prompt);

        return Task.FromResult(new PromptAnalysis
        {
            TokenCount = tokenCount,
            ComplexityScore = Math.Round(complexity, 2),
            IsWellFormed = isWellFormed
        });
    }

    private static int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var wordBoundaries = text.Split(new[] { ' ', '\n', '\t', '\r' },
            StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)Math.Ceiling(wordBoundaries * 1.3);
    }
}
