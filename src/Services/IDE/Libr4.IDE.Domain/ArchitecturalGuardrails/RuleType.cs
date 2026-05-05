namespace Libr4.IDE.Domain.ArchitecturalGuardrails;

/// <summary>
/// Represents the type of guardrail rule
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Naming convention rules
    /// </summary>
    Naming = 1,
    
    /// <summary>
    /// Code structure rules
    /// </summary>
    Structure = 2,
    
    /// <summary>
    /// Dependency rules
    /// </summary>
    Dependency = 3,
    
    /// <summary>
    /// Security rules
    /// </summary>
    Security = 4
}
