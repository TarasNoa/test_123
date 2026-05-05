using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AITranslate;

public enum TranslationQuality { Draft, Standard, Professional, Certified }
public enum TranslationStatus { Pending, Translating, Completed, Failed }

public class Translation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SourceLang { get; set; } = string.Empty;
    public string TargetLang { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;
    public string? TranslatedText { get; set; }
    public TranslationQuality Quality { get; set; } = TranslationQuality.Standard;
    public TranslationStatus Status { get; set; } = TranslationStatus.Pending;
    public float? ConfidenceScore { get; set; }
    public int TokensUsed { get; set; }
    public decimal Cost { get; set; }
    public string? Context { get; set; } // e.g. "technical", "legal", "casual"
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class LanguagePair
{
    public Guid Id { get; set; }
    public string SourceLang { get; set; } = string.Empty;
    public string TargetLang { get; set; } = string.Empty;
    public bool IsSupported { get; set; } = true;
    public float? AverageQualityScore { get; set; }
    public int TranslationCount { get; set; }
}
