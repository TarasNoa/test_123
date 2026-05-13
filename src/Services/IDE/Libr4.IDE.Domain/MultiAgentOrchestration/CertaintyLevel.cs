namespace Libr4.IDE.Domain.MultiAgentOrchestration;

public enum CertaintyLevel
{
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
/// Extension methods for CertaintyLevel
/// </summary>
public static class CertaintyLevelExtensions
{
    /// <summary>
    /// Get the minimum confidence score for a certainty level
    /// </summary>
    public static double MinConfidence(this CertaintyLevel level) => level switch
    {
        CertaintyLevel.VeryLow => 0.0,
        CertaintyLevel.Low => 0.2,
        CertaintyLevel.Medium => 0.4,
        CertaintyLevel.High => 0.6,
        CertaintyLevel.VeryHigh => 0.8,
        _ => 0.0
    };
    
    /// <summary>
    /// Get the maximum confidence score for a certainty level
    /// </summary>
    public static double MaxConfidence(this CertaintyLevel level) => level switch
    {
        CertaintyLevel.VeryLow => 0.2,
        CertaintyLevel.Low => 0.4,
        CertaintyLevel.Medium => 0.6,
        CertaintyLevel.High => 0.8,
        CertaintyLevel.VeryHigh => 1.0,
        _ => 1.0
    };
    
    /// <summary>
    /// Get certainty level from confidence score
    /// </summary>
    public static CertaintyLevel FromConfidence(double confidence)
    {
        return confidence switch
        {
            < 0.2 => CertaintyLevel.VeryLow,
            < 0.4 => CertaintyLevel.Low,
            < 0.6 => CertaintyLevel.Medium,
            < 0.8 => CertaintyLevel.High,
            _ => CertaintyLevel.VeryHigh
        };
    }
}
