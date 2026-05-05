namespace Libr4.IDE.Domain.IntelligenceRouter;

/// <summary>
/// Represents available AI model providers
/// </summary>
public enum ModelProvider
{
    /// <summary>
    /// OpenRouter - multi-provider routing for cost optimization
    /// </summary>
    OpenRouter = 1,
    
    /// <summary>
    /// Ollama - local models
    /// </summary>
    Ollama = 2,
    
    /// <summary>
    /// Anthropic - Claude models
    /// </summary>
    Anthropic = 3,
    
    /// <summary>
    /// OpenAI - GPT models
    /// </summary>
    OpenAI = 4,
    
    /// <summary>
    /// Google - Gemini models
    /// </summary>
    Google = 5,
    
    /// <summary>
    /// Together - open-source models
    /// </summary>
    Together = 6,
    
    /// <summary>
    /// Local - local inference
    /// </summary>
    Local = 7
}
