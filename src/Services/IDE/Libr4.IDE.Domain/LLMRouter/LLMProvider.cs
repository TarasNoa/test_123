namespace Libr4.IDE.Domain.LLMRouter;

/// <summary>
/// Represents the LLM provider
/// </summary>
public enum LLMProvider
{
    /// <summary>
    /// OpenAI provider
    /// </summary>
    OpenAI = 1,
    
    /// <summary>
    /// Anthropic provider
    /// </summary>
    Anthropic = 2,
    
    /// <summary>
    /// Local provider
    /// </summary>
    Local = 3,
    
    /// <summary>
    /// Azure OpenAI provider
    /// </summary>
    AzureOpenAI = 4
}
