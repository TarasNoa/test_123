using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AIExplanations;

public enum ExplanationType { CodeFunction, CodeBlock, Architecture, Algorithm, ErrorDiagnosis, BestPractice }
public enum DetailLevel { Brief, Standard, Detailed, Expert }

public class CodeExplanation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public ExplanationType Type { get; set; }
    public DetailLevel Level { get; set; } = DetailLevel.Standard;
    public string ExplanationText { get; set; } = string.Empty;
    public List<string> KeyConcepts { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
    public List<string> Suggestions { get; set; } = new List<string>();
    public int? UserRating { get; set; }
    public bool? WasHelpful { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class DecisionExplanation
{
    public Guid Id { get; set; }
    public Guid RelatedEntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public List<string> Factors { get; set; } = new List<string>();
    public float Confidence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Explanation
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
