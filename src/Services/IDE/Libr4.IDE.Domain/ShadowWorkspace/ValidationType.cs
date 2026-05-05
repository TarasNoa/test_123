namespace Libr4.IDE.Domain.ShadowWorkspace;

/// <summary>
/// Represents the type of validation
/// </summary>
public enum ValidationType
{
    /// <summary>
    /// AST validation
    /// </summary>
    AST = 1,
    
    /// <summary>
    /// Import smoke tests
    /// </summary>
    ImportSmoke = 2,
    
    /// <summary>
    /// Pytest execution
    /// </summary>
    Pytest = 3,
    
    /// <summary>
    /// Frontend type checking
    /// </summary>
    Typecheck = 4
}
