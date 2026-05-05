namespace Libr4.IDE.Domain.CodeReview;

/// <summary>
/// Types of code review analysis
/// </summary>
public enum ReviewType
{
    /// <summary>
    /// Pattern-based static analysis
    /// </summary>
    PatternAnalysis,
    
    /// <summary>
    /// AI-powered deep analysis
    /// </summary>
    AiAnalysis,
    
    /// <summary>
    /// Security vulnerability scan
    /// </summary>
    Security,
    
    /// <summary>
    /// Performance analysis
    /// </summary>
    Performance,
    
    /// <summary>
    /// Best practices compliance
    /// </summary>
    BestPractices,
    
    /// <summary>
    /// Architectural review
    /// </summary>
    Architecture
}
