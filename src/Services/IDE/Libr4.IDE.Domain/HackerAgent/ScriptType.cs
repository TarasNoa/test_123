namespace Libr4.IDE.Domain.HackerAgent;

/// <summary>
/// Represents the type of security script
/// </summary>
public enum ScriptType
{
    /// <summary>
    /// Python scripts
    /// </summary>
    Python = 1,
    
    /// <summary>
    /// Bash scripts
    /// </summary>
    Bash = 2,
    
    /// <summary>
    /// PowerShell scripts
    /// </summary>
    PowerShell = 3,
    
    /// <summary>
    /// JavaScript scripts
    /// </summary>
    JavaScript = 4
}
