namespace Libr4.IDE.Domain.LLMRouter;

/// <summary>
/// Entity representing an LLM model
/// </summary>
public class LLMModel
{
    public Guid Id { get; private set; }
    public string ModelName { get; private set; }
    public LLMProvider Provider { get; private set; }
    public double CostPer1KTokens { get; private set; }
    public int MaxTokens { get; private set; }
    public double LatencyMs { get; private set; }
    public Dictionary<string, object> Capabilities { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private LLMModel() { }
    
    public LLMModel(
        string modelName,
        LLMProvider provider,
        double costPer1KTokens,
        int maxTokens,
        double latencyMs,
        Dictionary<string, object>? capabilities = null)
    {
        Id = Guid.NewGuid();
        ModelName = modelName;
        Provider = provider;
        CostPer1KTokens = costPer1KTokens;
        MaxTokens = maxTokens;
        LatencyMs = latencyMs;
        Capabilities = capabilities ?? new Dictionary<string, object>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddCapability(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            Capabilities[key] = value;
        }
    }
    
    public static LLMModel Create(
        string modelName,
        LLMProvider provider,
        double costPer1KTokens,
        int maxTokens,
        double latencyMs,
        Dictionary<string, object>? capabilities = null)
    {
        return new LLMModel(modelName, provider, costPer1KTokens, maxTokens, latencyMs, capabilities);
    }
}
