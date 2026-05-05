using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Libr4.AI.Infrastructure.Hooks.BuiltIn;

/// <summary>
/// Hook that detects and removes AI-generated text patterns to make content more natural.
/// Based on Wikipedia's "Signs of AI writing" guide and Humanizer-zh patterns.
/// </summary>
public class HumanizerHook : IHook
{
    private readonly ILogger<HumanizerHook> _logger;
    private readonly HumanizerOptions _options;

    public HookType Type => HookType.PostToolUse;
    public string Name => "Humanizer";

    public HumanizerHook(ILogger<HumanizerHook> logger, HumanizerOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new HumanizerOptions();
    }

    public Task<HookResult> ExecuteAsync(HookContext context)
    {
        if (context.Result is not string text)
        {
            return Task.FromResult(new HookResult { ShouldContinue = true });
        }

        var originalText = text;
        var score = CalculateQualityScore(text);

        _logger.LogDebug("Humanizer: Original quality score: {Score}/50", score);

        if (score >= _options.MinimumAcceptableScore)
        {
            _logger.LogDebug("Humanizer: Text already meets quality threshold ({Score} >= {Threshold})", 
                score, _options.MinimumAcceptableScore);
            return Task.FromResult(new HookResult { ShouldContinue = true });
        }

        var humanizedText = HumanizeText(text);
        var newScore = CalculateQualityScore(humanizedText);

        _logger.LogInformation("Humanizer: Quality score improved from {OldScore} to {NewScore}/50", 
            score, newScore);

        context.Metadata["original_score"] = score.ToString();
        context.Metadata["new_score"] = newScore.ToString();
        context.Metadata["patterns_fixed"] = DetectPatterns(originalText).Count.ToString();

        return Task.FromResult(new HookResult 
        { 
            ShouldContinue = true,
            ModifiedResult = humanizedText
        });
    }

    /// <summary>
    /// Humanizes text by removing AI-generated patterns.
    /// </summary>
    private string HumanizeText(string text)
    {
        var result = text;

        // Apply pattern fixes in priority order
        result = RemoveCollaborativeTraces(result);
        result = RemoveKnowledgeCutoffDisclaimers(result);
        result = RemoveSycophanticTone(result);
        result = RemoveFillerPhrases(result);
        result = RemoveOverqualification(result);
        result = RemoveGenericPositiveConclusions(result);
        result = RemoveOverusedAIVocabulary(result);
        result = RemoveNegativeParallelism(result);
        result = RemoveRuleOfThree(result);
        result = RemovePromotionalLanguage(result);
        result = RemoveVagueAttribution(result);
        result = RemoveIngEndingAnalysis(result);
        result = RemoveOverusedDashes(result);
        result = RemoveEmojis(result);
        result = RemoveOutlineChallengesSection(result);

        return result.Trim();
    }

    /// <summary>
    /// Calculates quality score (1-50) based on 5 dimensions.
    /// </summary>
    private int CalculateQualityScore(string text)
    {
        var patterns = DetectPatterns(text);
        
        // Fewer patterns = higher score
        var patternPenalty = Math.Min(patterns.Count * 2, 50);
        
        // Base score starts at 50, subtract penalties
        var score = 50 - patternPenalty;

        // Additional dimension scoring
        score += ScoreDirectness(text);
        score += ScoreRhythm(text);
        score += ScoreTrustworthiness(text);
        score += ScoreAuthenticity(text);
        score += ScoreRefinement(text);

        return Math.Clamp(score, 1, 50);
    }

    /// <summary>
    /// Detects all AI patterns in text.
    /// </summary>
    private List<string> DetectPatterns(string text)
    {
        var patterns = new List<string>();

        if (HasCollaborativeTraces(text)) patterns.Add("CollaborativeTraces");
        if (HasKnowledgeCutoffDisclaimers(text)) patterns.Add("KnowledgeCutoff");
        if (HasSycophanticTone(text)) patterns.Add("SycophanticTone");
        if (HasFillerPhrases(text)) patterns.Add("FillerPhrases");
        if (HasOverqualification(text)) patterns.Add("Overqualification");
        if (HasGenericPositiveConclusions(text)) patterns.Add("GenericConclusions");
        if (HasOverusedAIVocabulary(text)) patterns.Add("AIVocabulary");
        if (HasNegativeParallelism(text)) patterns.Add("NegativeParallelism");
        if (HasRuleOfThree(text)) patterns.Add("RuleOfThree");
        if (HasPromotionalLanguage(text)) patterns.Add("PromotionalLanguage");
        if (HasVagueAttribution(text)) patterns.Add("VagueAttribution");
        if (HasIngEndingAnalysis(text)) patterns.Add("IngEndingAnalysis");
        if (HasOverusedDashes(text)) patterns.Add("OverusedDashes");
        if (HasEmojis(text)) patterns.Add("Emojis");
        if (HasOutlineChallengesSection(text)) patterns.Add("OutlineChallenges");

        return patterns;
    }

    #region Pattern Detection

    private static readonly string[] CollaborativePhrases = 
    {
        @"希望这对您有帮助", @"当然！", @"一定！", @"您说得完全正确", 
        @"您想要", @"请告诉我", @"这是一个", @"Hope this helps",
        @"Of course!", @"Certainly!", @"You're absolutely right"
    };

    private static readonly string[] KnowledgeCutoffPhrases = 
    {
        @"截至", @"根据我最后的训练更新", @"虽然具体细节有限", @"基于可用信息",
        @"As of", @"Based on my last training update", @"While specific details are limited"
    };

    private static readonly string[] SycophanticPhrases = 
    {
        @"好问题", @"您说得完全正确", @"这是一个复杂的话题", @"很好的观点",
        @"Great question", @"You're absolutely right", @"That's a great point"
    };

    private static readonly string[] FillerPhrases = 
    {
        @"为了实现这一目标", @"由于下雨的事实", @"在这个时间点", @"在您需要帮助的情况下",
        @"值得注意的是数据显示", @"To achieve this goal", @"Due to the fact that",
        @"At this point in time", @"In the event that you need help"
    };

    private static readonly string[] AIVocabulary = 
    {
        @"此外", @"至关重要", @"深入探讨", @"强调", @"持久的", @"增强", @"培养",
        @"获得", @"突出", @"相互作用", @"复杂", @"关键", @"格局", @"展示",
        @"Furthermore", @"Crucial", @"Deep dive", @"Emphasize", @"Enhance", @"Cultivate",
        @"Highlight", @"Interplay", @"Complex", @"Key", @"Landscape", @"Showcase"
    };

    private bool HasCollaborativeTraces(string text) => 
        CollaborativePhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    private bool HasKnowledgeCutoffDisclaimers(string text) => 
        KnowledgeCutoffPhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    private bool HasSycophanticTone(string text) => 
        SycophanticPhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    private bool HasFillerPhrases(string text) => 
        FillerPhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    private bool HasOverqualification(string text)
    {
        // Count qualifying words like "potentially", "possibly", "might"
        var qualifiers = new[] { "potentially", "possibly", "might", "could", "may", "perhaps" };
        var count = qualifiers.Count(q => Regex.Matches(text.ToLower(), @"\b" + q + @"\b").Count > 2);
        return count > 0;
    }

    private bool HasGenericPositiveConclusions(string text)
    {
        var genericPhrases = new[] 
        { 
            @"未来看起来光明", @"激动人心的时代即将到来", @"继续追求卓越的旅程",
            @"向正确方向迈出的重要一步", @"The future looks bright", @"Exciting times ahead"
        };
        return genericPhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasOverusedAIVocabulary(string text)
    {
        var count = AIVocabulary.Count(v => 
            Regex.Matches(text.ToLower(), @"\b" + v.ToLower() + @"\b").Count > 0);
        return count >= 3;
    }

    private bool HasNegativeParallelism(string text)
    {
        // Patterns like "not just X, but Y" or "not only X, but also Y"
        return Regex.IsMatch(text, @"not\s+(just|only)\s+.*,\s+but\s+(also\s+)?", 
            RegexOptions.IgnoreCase);
    }

    private bool HasRuleOfThree(string text)
    {
        // Detect lists of three adjectives or nouns
        return Regex.IsMatch(text, @"\b\w+\s*,\s*\w+\s*,\s+and\s+\w+\b");
    }

    private bool HasPromotionalLanguage(string text)
    {
        var promoWords = new[] 
        { 
            @"无缝", @"直观", @"强大", @"充满活力", @"丰富的", @"令人叹为观止",
            @"seamless", @"intuitive", @"powerful", @"vibrant", @"rich", @"breathtaking"
        };
        return promoWords.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasVagueAttribution(string text)
    {
        var vagueSources = new[] 
        { 
            @"行业报告显示", @"观察者指出", @"专家认为", @"一些批评者认为",
            @"Industry reports show", @"Observers note", @"Experts believe"
        };
        return vagueSources.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasIngEndingAnalysis(string text)
    {
        // Detect phrases ending with -ing that add虚假深度
        return Regex.IsMatch(text, @"\b\w+ing\s+(?:by|through|with|for)\s+\w+", 
            RegexOptions.IgnoreCase);
    }

    private bool HasOverusedDashes(string text)
    {
        var dashCount = text.Count(c => c == '—');
        return dashCount > 2;
    }

    private bool HasEmojis(string text)
    {
        return Regex.IsMatch(text, @"[\p{So}]");
    }

    private bool HasOutlineChallengesSection(string text)
    {
        var challengePhrases = new[] 
        { 
            @"面临若干挑战", @"尽管存在这些挑战", @"挑战与遗产", @"未来展望",
            @"faces several challenges", @"Despite these challenges", @"Challenges and legacy"
        };
        return challengePhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Pattern Removal

    private string RemoveCollaborativeTraces(string text)
    {
        foreach (var phrase in CollaborativePhrases)
        {
            text = Regex.Replace(text, Regex.Escape(phrase) + @"[\.\,]?\s*", "", RegexOptions.IgnoreCase);
        }
        return text;
    }

    private string RemoveKnowledgeCutoffDisclaimers(string text)
    {
        foreach (var phrase in KnowledgeCutoffPhrases)
        {
            text = Regex.Replace(text, Regex.Escape(phrase) + @"[^.]*\.", "", RegexOptions.IgnoreCase);
        }
        return text;
    }

    private string RemoveSycophanticTone(string text)
    {
        foreach (var phrase in SycophanticPhrases)
        {
            text = Regex.Replace(text, Regex.Escape(phrase) + @"[\.\,]?\s*", "", RegexOptions.IgnoreCase);
        }
        return text;
    }

    private string RemoveFillerPhrases(string text)
    {
        foreach (var phrase in FillerPhrases)
        {
            text = Regex.Replace(text, Regex.Escape(phrase), "", RegexOptions.IgnoreCase);
        }
        return text;
    }

    private string RemoveOverqualification(string text)
    {
        // Remove excessive qualifying words
        text = Regex.Replace(text, @"\b(potentially|possibly|perhaps)\s+", "", RegexOptions.IgnoreCase);
        return text;
    }

    private string RemoveGenericPositiveConclusions(string text)
    {
        foreach (var phrase in new[] { @"未来看起来光明", @"The future looks bright", @"Exciting times ahead" })
        {
            text = Regex.Replace(text, Regex.Escape(phrase) + @"[^.]*\.", "", RegexOptions.IgnoreCase);
        }
        return text;
    }

    private string RemoveOverusedAIVocabulary(string text)
    {
        foreach (var word in AIVocabulary)
        {
            text = Regex.Replace(text, @"\b" + Regex.Escape(word) + @"\b", "", RegexOptions.IgnoreCase);
        }
        return text;
    }

    private string RemoveNegativeParallelism(string text)
    {
        // Simplify "not just X, but Y" to "Y"
        text = Regex.Replace(text, @"not\s+(just|only)\s+[^,]+,\s+but\s+(also\s+)?", "", RegexOptions.IgnoreCase);
        return text;
    }

    private string RemoveRuleOfThree(string text)
    {
        // Simplify three-item lists to two items
        text = Regex.Replace(text, @"(\w+),\s*(\w+),\s+and\s+(\w+)", "$1, $2, and $3");
        return text;
    }

    private string RemovePromotionalLanguage(string text)
    {
        var promoWords = new[] { @"无缝", @"直观", @"强大", @"seamless", @"intuitive", @"powerful" };
        foreach (var word in promoWords)
        {
            text = Regex.Replace(text, @"\b" + Regex.Escape(word) + @"\b", "", RegexOptions.IgnoreCase);
        }
        return text;
    }

    private string RemoveVagueAttribution(string text)
    {
        foreach (var phrase in new[] { @"行业报告显示", @"Experts believe", @"Observers note" })
        {
            text = Regex.Replace(text, Regex.Escape(phrase) + @"[^.]*\.", "", RegexOptions.IgnoreCase);
        }
        return text;
    }

    private string RemoveIngEndingAnalysis(string text)
    {
        // Remove superficial -ing phrases
        text = Regex.Replace(text, @"\b\w+ing\s+(?:by|through|with|for)\s+\w+\s*,?\s*", "", RegexOptions.IgnoreCase);
        return text;
    }

    private string RemoveOverusedDashes(string text)
    {
        // Replace em-dashes with commas or periods
        text = text.Replace('—', ',');
        return text;
    }

    private string RemoveEmojis(string text)
    {
        return Regex.Replace(text, @"[\p{So}]", "");
    }

    private string RemoveOutlineChallengesSection(string text)
    {
        foreach (var phrase in new[] { @"面临若干挑战", @"faces several challenges" })
        {
            text = Regex.Replace(text, Regex.Escape(phrase) + @"[^.]*\.?\s*", "", RegexOptions.IgnoreCase);
        }
        return text;
    }

    #endregion

    #region Quality Scoring Dimensions

    private int ScoreDirectness(string text)
    {
        // Fewer filler phrases = higher directness
        var fillerCount = FillerPhrases.Count(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
        return Math.Max(0, 10 - fillerCount * 2);
    }

    private int ScoreRhythm(string text)
    {
        // Varied sentence lengths = higher rhythm
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        if (sentences.Length < 2) return 5;

        var lengths = sentences.Select(s => s.Split().Length).ToList();
        var variance = lengths.Count > 0 ? lengths.Max() - lengths.Min() : 0;
        return Math.Min(10, variance);
    }

    private int ScoreTrustworthiness(string text)
    {
        // Fewer vague attributions = higher trust
        var vagueCount = new[] { @"专家认为", @"Experts believe", @"Observers note" }
            .Count(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
        return Math.Max(0, 10 - vagueCount * 3);
    }

    private int ScoreAuthenticity(string text)
    {
        // Fewer AI vocabulary words = higher authenticity
        var aiVocabCount = AIVocabulary.Count(v => text.Contains(v, StringComparison.OrdinalIgnoreCase));
        return Math.Max(0, 10 - aiVocabCount);
    }

    private int ScoreRefinement(string text)
    {
        // Shorter, more concise text = higher refinement
        var wordCount = text.Split().Length;
        return Math.Max(0, 10 - (wordCount / 50));
    }

    #endregion
}

/// <summary>
/// Configuration options for the Humanizer hook.
/// </summary>
public class HumanizerOptions
{
    /// <summary>
    /// Minimum acceptable quality score (1-50). Text below this will be humanized.
    /// Default: 35
    /// </summary>
    public int MinimumAcceptableScore { get; set; } = 35;

    /// <summary>
    /// Whether to enable strict mode (more aggressive pattern removal).
    /// Default: false
    /// </summary>
    public bool StrictMode { get; set; } = false;

    /// <summary>
    /// Whether to log detailed pattern detection information.
    /// Default: false
    /// </summary>
    public bool VerboseLogging { get; set; } = false;
}
