namespace Libr4.IDE.Domain.WebSearch;

/// <summary>
/// Represents the search provider
/// </summary>
public enum SearchProvider
{
    /// <summary>
    /// Tavily provider
    /// </summary>
    Tavily = 1,
    
    /// <summary>
    /// Brave provider
    /// </summary>
    Brave = 2,
    
    /// <summary>
    /// SerpAPI provider
    /// </summary>
    SerpAPI = 3,
    
    /// <summary>
    /// DuckDuckGo provider
    /// </summary>
    DuckDuckGo = 4
}
