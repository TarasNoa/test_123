namespace Libr4.IDE.Domain.IntelligenceRouter;

/// <summary>
/// Represents available external tools for context enrichment
/// </summary>
public enum ToolType
{
    /// <summary>
    /// Browser web search
    /// </summary>
    BrowserSearch = 1,
    
    /// <summary>
    /// GitHub repository search
    /// </summary>
    GitHubSearch = 2,
    
    /// <summary>
    /// StackOverflow search
    /// </summary>
    StackOverflowSearch = 3,
    
    /// <summary>
    /// Documentation search
    /// </summary>
    DocumentationSearch = 4,
    
    /// <summary>
    /// No external tools
    /// </summary>
    None = 0
}
