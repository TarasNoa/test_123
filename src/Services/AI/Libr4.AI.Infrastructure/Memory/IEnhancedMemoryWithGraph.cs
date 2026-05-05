namespace Libr4.AI.Infrastructure.Memory;

/// <summary>
/// Base memory interface
/// </summary>
public interface IEnhancedMemory
{
    Task AddMemoryAsync(string userId, string content, Dictionary<string, string>? metadata = null);
    Task<List<MemoryItem>> RetrieveAsync(string userId, string query, int topK = 5);
    Task DeleteMemoryAsync(string userId, string memoryId);
    Task ClearMemoriesAsync(string userId);
}

public class MemoryItem
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float Similarity { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Enhanced memory with Hebbian associative graph
/// Combines vector similarity with concept associations
/// </summary>
public interface IEnhancedMemoryWithGraph : IEnhancedMemory
{
    /// <summary>
    /// Add memory with concept extraction for graph
    /// </summary>
    Task AddMemoryWithConceptsAsync(string userId, string content, List<string> concepts);
    
    /// <summary>
    /// Retrieve with Hebbian boost
    /// </summary>
    Task<List<MemoryItem>> RetrieveWithBoostAsync(string userId, string query, int topK = 5);
    
    /// <summary>
    /// Get associated concepts for a query
    /// </summary>
    Task<List<string>> GetAssociatedConceptsAsync(string userId, string query);
    
    /// <summary>
    /// Get consolidation status for memory items
    /// </summary>
    Task<Dictionary<string, ConsolidationStatus>> GetConsolidationStatusAsync(string userId);
}
