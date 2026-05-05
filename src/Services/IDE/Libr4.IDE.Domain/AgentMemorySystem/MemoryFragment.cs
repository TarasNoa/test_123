namespace Libr4.IDE.Domain.AgentMemorySystem;

/// <summary>
/// Entity representing a memory fragment
/// Enhanced with temporal knowledge graph concepts from OpenMemory
/// </summary>
public class MemoryFragment
{
    public Guid Id { get; private set; }
    public string Content { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }
    public float RelevanceScore { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    
    /// <summary>
    /// Valid from timestamp (for temporal reasoning - from OpenMemory)
    /// </summary>
    public DateTime? ValidFrom { get; private set; }
    
    /// <summary>
    /// Valid to timestamp (for temporal reasoning - from OpenMemory)
    /// When set, this fragment is only valid between ValidFrom and ValidTo
    /// </summary>
    public DateTime? ValidTo { get; private set; }
    
    /// <summary>
    /// Confidence score for this fragment (0-1)
    /// </summary>
    public float Confidence { get; private set; }
    
    /// <summary>
    /// Salience score (importance) for this fragment
    /// </summary>
    public float Salience { get; private set; }
    
    /// <summary>
    /// Whether this fragment is currently valid based on temporal constraints
    /// </summary>
    public bool IsCurrentlyValid => IsValidAt(DateTime.UtcNow);
    
    private MemoryFragment() { }
    
    public MemoryFragment(
        string content,
        Dictionary<string, object>? metadata = null,
        DateTime? expiresAt = null,
        DateTime? validFrom = null,
        DateTime? validTo = null)
    {
        Id = Guid.NewGuid();
        Content = content;
        Metadata = metadata ?? new Dictionary<string, object>();
        RelevanceScore = 1.0f;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        ValidFrom = validFrom ?? CreatedAt;
        ValidTo = validTo;
        Confidence = 1.0f;
        Salience = 1.0f;
    }
    
    public void SetRelevanceScore(float score)
    {
        RelevanceScore = Math.Max(0.0f, Math.Min(1.0f, score));
    }
    
    /// <summary>
    /// Set confidence score (from OpenMemory)
    /// </summary>
    public void SetConfidence(float confidence)
    {
        Confidence = Math.Max(0.0f, Math.Min(1.0f, confidence));
    }
    
    /// <summary>
    /// Set salience score (importance) (from OpenMemory)
    /// </summary>
    public void SetSalience(float salience)
    {
        Salience = Math.Max(0.0f, Math.Min(1.0f, salience));
    }
    
    /// <summary>
    /// Add metadata
    /// </summary>
    public void AddMetadata(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            Metadata[key] = value;
        }
    }
    
    /// <summary>
    /// Check if this fragment is valid at a specific point in time (from OpenMemory)
    /// </summary>
    public bool IsValidAt(DateTime timestamp)
    {
        if (ValidFrom.HasValue && timestamp < ValidFrom.Value)
            return false;
        
        if (ValidTo.HasValue && timestamp > ValidTo.Value)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Check if this fragment has expired
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    }
    
    /// <summary>
    /// Close this fragment temporally (sets ValidTo to now)
    /// </summary>
    public void CloseTemporally()
    {
        ValidTo = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Composite score combining relevance, confidence, and salience (from OpenMemory)
    /// </summary>
    public float GetCompositeScore()
    {
        return (RelevanceScore * 0.4f) + (Confidence * 0.3f) + (Salience * 0.3f);
    }
    
    public static MemoryFragment Create(
        string content,
        Dictionary<string, object>? metadata = null,
        DateTime? expiresAt = null,
        DateTime? validFrom = null,
        DateTime? validTo = null)
    {
        return new MemoryFragment(content, metadata, expiresAt, validFrom, validTo);
    }
}
