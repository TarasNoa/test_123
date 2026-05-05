namespace Libr4.IDE.Domain.IntelligenceRouter;

/// <summary>
/// Represents the complexity level of a phase for routing decisions
/// </summary>
public enum PhaseComplexity
{
    /// <summary>
    /// Simple phase, lightweight model sufficient
    /// </summary>
    Low = 1,
    
    /// <summary>
    /// Moderate complexity, standard model
    /// </summary>
    Medium = 2,
    
    /// <summary>
    /// Complex phase, advanced model required
    /// </summary>
    High = 3,
    
    /// <summary>
    /// Safety-critical, highest quality model required
    /// </summary>
    Critical = 4
}
