namespace Libr4.AI.Application.Memory;

/// <summary>
/// Graph-based memory store for storing memories with entity relationships.
/// Enables complex graph queries and path traversal.
/// </summary>
public interface IGraphMemoryStore
{
    Task InitializeAsync(CancellationToken ct = default);
    
    Task StoreMemoryAsync(
        MemoryNode memory,
        IEnumerable<EntityLink>? entityLinks = null,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<MemoryNode>> GetMemoriesByEmbeddingIdAsync(
        string embeddingId,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<MemoryNode>> SearchByEntityPathAsync(
        string startEntityId,
        int depth = 2,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<MemoryNode>> GetRelatedMemoriesAsync(
        string memoryId,
        int minSharedEntities = 1,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<MemoryQueryResult>> QueryGraphAsync(
        string cypherQuery,
        Dictionary<string, object>? parameters = null,
        CancellationToken ct = default);
    
    Task DeleteMemoryAsync(string memoryId, CancellationToken ct = default);
    
    Task<GraphStatistics> GetStatisticsAsync(CancellationToken ct = default);
}

/// <summary>
/// A memory node stored in the graph database.
/// </summary>
public sealed record MemoryNode(
    string Id,
    string Content,
    string Type,
    MemoryLevel Level,
    string? UserId = null,
    string? SessionId = null,
    string? AgentId = null,
    DateTime CreatedAt = default,
    string? EmbeddingId = null,
    float Importance = 1.0f,
    Dictionary<string, string>? Metadata = null);

/// <summary>
/// Memory level determines scope and retention policy.
/// </summary>
public enum MemoryLevel
{
    /// <summary>
    /// Long-term user preferences and knowledge.
    /// </summary>
    User,
    
    /// <summary>
    /// Session-specific context.
    /// </summary>
    Session,
    
    /// <summary>
    /// Agent-specific internal knowledge.
    /// </summary>
    Agent
}

/// <summary>
/// Result from a graph query.
/// </summary>
public sealed record MemoryQueryResult(
    MemoryNode Memory,
    object? Path,
    double Score);

/// <summary>
/// Statistics about the graph database.
/// </summary>
public sealed record GraphStatistics(
    int MemoryNodeCount,
    int EntityNodeCount,
    int RelationshipCount,
    DateTime LastUpdated);
