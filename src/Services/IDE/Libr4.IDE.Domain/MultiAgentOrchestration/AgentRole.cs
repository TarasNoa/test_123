namespace Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Represents the role of an agent in multi-agent orchestration
/// </summary>
public enum AgentRole
{
    /// <summary>
    /// Executes code changes
    /// </summary>
    Executor = 1,
    
    /// <summary>
    /// Code linting and style checks
    /// </summary>
    Linter = 2,
    
    /// <summary>
    /// Security reviews
    /// </summary>
    Security = 3,
    
    /// <summary>
    /// Code reviews
    /// </summary>
    Reviewer = 4,
    
    /// <summary>
    /// Testing
    /// </summary>
    Tester = 5,
    
    /// <summary>
    /// Architecture reviews
    /// </summary>
    Architect = 6,
    
    /// <summary>
    /// Debugging
    /// </summary>
    Debugger = 7
}
