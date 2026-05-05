namespace Libr4.AI.Infrastructure.Exoskeleton;

/// <summary>
/// Exoskeleton Protocol - LLM verification to prevent hallucinations
/// Based on Claude Octopus pattern with quality gate
/// </summary>
public interface IExoskeletonProtocol
{
    /// <summary>
    /// Apply exoskeleton to prompt (enhance with verification instructions)
    /// </summary>
    Task<string> ApplyExoskeletonAsync(string prompt);
    
    /// <summary>
    /// Verify response against exoskeleton rules
    /// </summary>
    Task<ExoskeletonVerificationResult> VerifyResponseAsync(string response, string? originalPrompt = null);
    
    /// <summary>
    /// Check if response passes quality gate (75% consensus)
    /// </summary>
    Task<QualityGateResult> CheckQualityGateAsync(List<string> responses, float threshold = 0.75f);
    
    /// <summary>
    /// Extract confidence level from response
    /// </summary>
    Task<ConfidenceLevel> ExtractConfidenceAsync(string response);
    
    /// <summary>
    /// Get question form for clarification
    /// </summary>
    Task<string> GetQuestionFormAsync(string topic);
    
    /// <summary>
    /// Process question form response
    /// </summary>
    Task<string> ProcessQuestionFormAsync(string responses);
}

/// <summary>
/// Severity levels for issues
/// </summary>
public enum Severity
{
    Info,
    Warning,
    Error,
    Critical
}

public class ExoskeletonOptions
{
    public bool RequireConfidenceMarking { get; set; } = true;
    public bool RequireSourceCitation { get; set; } = true;
    public bool AllowUncertainty { get; set; } = true;
    public bool DetectPromptInjection { get; set; } = true;
    public List<string> ForbiddenTopics { get; set; } = new();
}

public class QualityGateResult
{
    public bool PassesThreshold { get; set; }
    public float AgreementLevel { get; set; }
    public string? DominantAnswer { get; set; }
    public List<string> AgreeingResponses { get; set; } = new();
    public List<string> DisagreeingResponses { get; set; } = new();
}

public class ExoskeletonVerificationResult
{
    public bool IsSafe { get; set; }
    public List<VerificationIssue> Issues { get; set; } = new();
    public float Confidence { get; set; }
    public string? SuggestedFix { get; set; }
}

public class VerificationIssue
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Severity Severity { get; set; }
}

public enum ConfidenceLevel
{
    Unknown,
    High,
    Medium,
    Low,
    Guessing
}
