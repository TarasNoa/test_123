namespace Libr4.IDE.Domain.CodeIntelligence;

/// <summary>
/// Represents the type of code completion
/// </summary>
public enum CompletionType
{
    /// <summary>
    /// Language keywords
    /// </summary>
    Keyword = 1,
    
    /// <summary>
    /// Variable names
    /// </summary>
    Variable = 2,
    
    /// <summary>
    /// Function names
    /// </summary>
    Function = 3,
    
    /// <summary>
    /// Type names
    /// </summary>
    Type = 4,
    
    /// <summary>
    /// Property names
    /// </summary>
    Property = 5,
    
    /// <summary>
    /// Method names
    /// </summary>
    Method = 6
}
