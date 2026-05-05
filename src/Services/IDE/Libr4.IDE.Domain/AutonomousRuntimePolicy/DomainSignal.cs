namespace Libr4.IDE.Domain.AutonomousRuntimePolicy;

/// <summary>
/// Represents domain signals for runtime policy
/// </summary>
public enum DomainSignal
{
    /// <summary>
    /// Standard production SaaS
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Banking/fintech, healthcare
    /// </summary>
    Regulated = 1,
    
    /// <summary>
    /// Spacecraft, medical devices, industrial control
    /// </summary>
    SafetyCritical = 2
}
