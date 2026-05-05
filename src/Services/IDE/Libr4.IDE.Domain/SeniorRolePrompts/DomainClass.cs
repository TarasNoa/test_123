namespace Libr4.IDE.Domain.SeniorRolePrompts;

/// <summary>
/// Represents the domain class of the project
/// </summary>
public enum DomainClass
{
    /// <summary>
    /// Production SaaS - reliability, clarity, and maintainability over demos
    /// </summary>
    Standard = 1,
    
    /// <summary>
    /// Banking/fintech, healthcare - safety, auditability, and truthful logic are mandatory
    /// </summary>
    Regulated = 2,
    
    /// <summary>
    /// Spacecraft, medical devices, industrial control - approvals, audit trails, explicit failure modes
    /// </summary>
    SafetyCritical = 3
}
