/*
using System.Text.Json;
using Libr4.AI.Application.Memory;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

namespace Libr4.AI.Infrastructure.Memory;

/// <summary>
/// Production-ready hybrid memory service combining:
/// - Vector search (semantic similarity via Qdrant)
/// - Graph storage (entity relationships via Neo4j)
/// - Multi-level memory (User, Session, Agent scopes)
/// - Entity extraction and linking
/// 
/// Inspired by mem0 and cognee architectures.
/// </summary>
public sealed class HybridMemoryService : IHybridMemoryService
{
    private readonly IVectorMemoryStore _vectorStore;
    private readonly IGraphMemoryStore _graphStore;
    private readonly IEmbeddingsService _embeddings;
    private readonly IEntityExtractor _entityExtractor;
    private readonly ILogger<HybridMemoryService> _logger;

    public HybridMemoryService(
        IVectorMemoryStore vectorStore,
        IGraphMemoryStore graphStore,
        IEmbeddingsService embeddings,
        IEntityExtractor entityExtractor,
        ILogger<HybridMemoryService> logger)
    {
        _vectorStore = vectorStore;
        _graphStore = graphStore;
        _embeddings = embeddings;
        _entityExtractor = entityExtractor;
        _logger = logger;
    }

    /// <summary>
    /// Initialize all memory stores.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing hybrid memory service...");
        
        if (_vectorStore is IAsyncDisposable vectorDisposable)
        {
            // Qdrant initialization
            if (vectorDisposable.GetType().GetMethod("InitializeAsync") != null)
            {
                await ((dynamic)_vectorStore).InitializeAsync(ct);
            }
        }
        
        await _graphStore.InitializeAsync(ct);
        
        _logger.LogInformation("Hybrid memory service initialized");
    }

    /// <summary>
    /// Store a memory across all stores (vector + graph).
    /// </summary>
    public async Task<MemoryEntry> RememberAsync(
        string content,
        MemoryLevel level,
        MemoryOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new MemoryOptions();
        var memoryId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow;
        
        _logger.LogDebug("Storing memory {MemoryId} at level {Level}", memoryId, level);

        // 1. Generate embedding
        var embedding = await _embeddings.GenerateEmbeddingAsync(content, cancellationToken: ct);

        // 2. Extract entities
        var extractionResult = await _entityExtractor.ExtractWithRelationshipsAsync(
            content,
            new ExtractionOptions 
            { 
                UserId = options.UserId,
                MinConfidence = 0.7f,
                ExtractRelationships = true
            },
            ct);

        // 3. Store in vector database
        var vectorRecord = new VectorRecord(
            Id: memoryId,
            CollectionId: GetCollectionId(level, options.UserId, options.SessionId),
            Embedding: embedding,
            Text: content,
            Metadata: new Dictionary<string, string>
            {
                ["level"] = level.ToString(),
                ["type"] = options.Type ?? "general",
                ["user_id"] = options.UserId ?? "",
                ["session_id"] = options.SessionId ?? "",
                ["agent_id"] = options.AgentId ?? "",
                ["created_at"] = timestamp.ToString("O"),
                ["importance"] = options.Importance.ToString()
            }
        );

        await _vectorStore.UpsertAsync(vectorRecord, ct);

        // 4. Store in graph database with entity links
        var entityLinks = extractionResult.Entities
            .Select(e => new EntityLink(
                EntityId: e.Id,
                EntityName: e.Name,
                EntityType: e.Type,
                RelationshipType: "mentions",
                Weight: e.Confidence))
            .ToList();

        var memoryNode = new MemoryNode(
            Id: memoryId,
            Content: content,
            Type: options.Type ?? "general",
            Level: level,
            UserId: options.UserId,
            SessionId: options.SessionId,
            AgentId: options.AgentId,
            CreatedAt: timestamp,
            EmbeddingId: memoryId, // Same as memory ID for correlation
            Importance: options.Importance,
            Metadata: options.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
        );

        await _graphStore.StoreMemoryAsync(memoryNode, entityLinks, ct);

        // 5. Create and return entry
        var entry = new MemoryEntry(
            Id: memoryId,
            Content: content,
            Level: level,
            UserId: options.UserId,
            SessionId: options.SessionId,
            AgentId: options.AgentId,
            CreatedAt: timestamp,
            Type: options.Type ?? "general",
            Importance: options.Importance,
            Entities: extractionResult.Entities,
            Metadata: options.Metadata
        );

        _logger.LogInformation(
            "Stored memory {MemoryId} with {EntityCount} entities at level {Level}",
            memoryId, entityLinks.Count, level);

        return entry;
    }

    /// <summary>
    /// Recall memories using hybrid search (vector + graph + keyword).
    /// </summary>
    public async Task<IReadOnlyList<MemoryRecallResult>> RecallAsync(
        string query,
        RecallOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new RecallOptions();
        
        _logger.LogDebug("Recalling memories for query: {Query}", query);

        // 1. Generate query embedding
        var queryEmbedding = await _embeddings.GenerateEmbeddingAsync(query, cancellationToken: ct);

        // 2. Search vector store
        var vectorResults = await _vectorStore.SearchAsync(
            queryEmbedding,
            collectionId: options.SessionId != null ? $"session_{options.SessionId}" : null,
            topK: options.TopK * 2, // Get more candidates for reranking
            minScore: options.MinSemanticScore,
            ct: ct);

        // 3. Get corresponding graph memories with entity context
        var graphResults = new List<MemoryNode>();
        foreach (var vectorResult in vectorResults)
        {
            var graphMemories = await _graphStore.GetMemoriesByEmbeddingIdAsync(
                vectorResult.Record.Id, ct);
            graphResults.AddRange(graphMemories);
        }

        // 4. Search by entity if entities exist in query
        var extractedEntities = await _entityExtractor.ExtractEntitiesAsync(query, ct: ct);
        foreach (var entity in extractedEntities.Where(e => e.Confidence > 0.8f))
        {
            var entityPathResults = await _graphStore.SearchByEntityPathAsync(
                entity.Id, 
                depth: options.EntitySearchDepth,
                ct: ct);
            graphResults.AddRange(entityPathResults);
        }

        // 5. Combine and deduplicate results
        var combinedResults = new List<MemoryRecallResult>();
        var seenIds = new HashSet<string>();

        // Add vector results with scores
        foreach (var vectorResult in vectorResults)
        {
            if (seenIds.Add(vectorResult.Record.Id))
            {
                var graphMemory = graphResults.FirstOrDefault(m => m.EmbeddingId == vectorResult.Record.Id);
                combinedResults.Add(new MemoryRecallResult(
                    Id: vectorResult.Record.Id,
                    Content: vectorResult.Record.Text,
                    Level: graphMemory?.Level ?? MemoryLevel.User,
                    SemanticScore: vectorResult.Score,
                    GraphScore: 0,
                    CombinedScore: vectorResult.Score,
                    Source: "vector",
                    CreatedAt: graphMemory?.CreatedAt ?? DateTime.MinValue,
                    Metadata: vectorResult.Record.Metadata?.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => (object)kvp.Value) ?? new Dictionary<string, object>()
                ));
            }
        }

        // Add graph results not already included
        foreach (var graphMemory in graphResults)
        {
            if (seenIds.Add(graphMemory.Id))
            {
                combinedResults.Add(new MemoryRecallResult(
                    Id: graphMemory.Id,
                    Content: graphMemory.Content,
                    Level: graphMemory.Level,
                    SemanticScore: 0,
                    GraphScore: 1.0, // High relevance through entity connection
                    CombinedScore: 0.7, // Lower than direct semantic match
                    Source: "graph",
                    CreatedAt: graphMemory.CreatedAt,
                    Metadata: graphMemory.Metadata?.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => (object)kvp.Value) ?? new Dictionary<string, object>()
                ));
            }
        }

        // 6. Apply level-based filtering and boosting
        var filteredResults = ApplyLevelFiltering(combinedResults, options);

        // 7. Rerank by combined score
        var rankedResults = filteredResults
            .OrderByDescending(r => r.CombinedScore)
            .ThenByDescending(r => r.CreatedAt)
            .Take(options.TopK)
            .ToList();

        _logger.LogInformation(
            "Recalled {Count} memories for query (from {VectorCount} vector + {GraphCount} graph)",
            rankedResults.Count, vectorResults.Count, graphResults.Count);

        return rankedResults;
    }

    /// <summary>
    /// Get memories by specific session.
    /// </summary>
    public async Task<IReadOnlyList<MemoryEntry>> GetSessionMemoriesAsync(
        string sessionId,
        int limit = 100,
        CancellationToken ct = default)
    {
        var collectionId = $"session_{sessionId}";
        
        // Query graph for session memories
        var query = @"
            MATCH (m:Memory)
            WHERE m.sessionId = $sessionId
            RETURN m
            ORDER BY m.createdAt DESC
            LIMIT $limit";

        var results = await _graphStore.QueryGraphAsync(
            query,
            new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["limit"] = limit
            },
            ct);

        return results.Select(r => new MemoryEntry(
            Id: r.Memory.Id,
            Content: r.Memory.Content,
            Level: r.Memory.Level,
            UserId: r.Memory.UserId,
            SessionId: r.Memory.SessionId,
            AgentId: r.Memory.AgentId,
            CreatedAt: r.Memory.CreatedAt,
            Type: r.Memory.Type,
            Importance: r.Memory.Importance,
            Metadata: r.Memory.Metadata?.ToDictionary(
                kvp => kvp.Key, 
                kvp => (object)kvp.Value)
        )).ToList();
    }

    /// <summary>
    /// Get user-level memories.
    /// </summary>
    public async Task<IReadOnlyList<MemoryEntry>> GetUserMemoriesAsync(
        string userId,
        int limit = 100,
        CancellationToken ct = default)
    {
        var query = @"
            MATCH (m:Memory)
            WHERE m.userId = $userId AND m.level = 'User'
            RETURN m
            ORDER BY m.importance DESC, m.createdAt DESC
            LIMIT $limit";

        var results = await _graphStore.QueryGraphAsync(
            query,
            new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["limit"] = limit
            },
            ct);

        return results.Select(r => new MemoryEntry(
            Id: r.Memory.Id,
            Content: r.Memory.Content,
            Level: r.Memory.Level,
            UserId: r.Memory.UserId,
            SessionId: r.Memory.SessionId,
            AgentId: r.Memory.AgentId,
            CreatedAt: r.Memory.CreatedAt,
            Type: r.Memory.Type,
            Importance: r.Memory.Importance,
            Metadata: r.Memory.Metadata?.ToDictionary(
                kvp => kvp.Key, 
                kvp => (object)kvp.Value)
        )).ToList();
    }

    /// <summary>
    /// Get related memories through graph connections.
    /// </summary>
    public async Task<IReadOnlyList<MemoryEntry>> GetRelatedMemoriesAsync(
        string memoryId,
        int minSharedEntities = 1,
        CancellationToken ct = default)
    {
        var related = await _graphStore.GetRelatedMemoriesAsync(
            memoryId, 
            minSharedEntities, 
            ct);

        return related.Select(m => new MemoryEntry(
            Id: m.Id,
            Content: m.Content,
            Level: m.Level,
            UserId: m.UserId,
            SessionId: m.SessionId,
            AgentId: m.AgentId,
            CreatedAt: m.CreatedAt,
            Type: m.Type,
            Importance: m.Importance,
            Metadata: m.Metadata?.ToDictionary(
                kvp => kvp.Key, 
                kvp => (object)kvp.Value)
        )).ToList();
    }

    /// <summary>
    /// Delete a memory from all stores.
    /// </summary>
    public async Task ForgetAsync(string memoryId, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting memory {MemoryId}", memoryId);

        // Delete from vector store
        if (_vectorStore is QdrantVectorMemoryStore qdrant)
        {
            await qdrant.DeleteAsync(memoryId, ct: ct);
        }

        // Delete from graph store
        await _graphStore.DeleteMemoryAsync(memoryId, ct);

        _logger.LogInformation("Deleted memory {MemoryId}", memoryId);
    }

    /// <summary>
    /// Get memory system statistics.
    /// </summary>
    public async Task<HybridMemoryStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var graphStats = await _graphStore.GetStatisticsAsync(ct);
        
        return new HybridMemoryStatistics(
            VectorRecords: graphStats.MemoryNodeCount, // Approximate
            GraphNodes: graphStats.MemoryNodeCount + graphStats.EntityNodeCount,
            GraphRelationships: graphStats.RelationshipCount,
            LastUpdated: graphStats.LastUpdated
        );
    }

    private string GetCollectionId(MemoryLevel level, string? userId, string? sessionId)
    {
        return level switch
        {
            MemoryLevel.User => $"user_{userId ?? "default"}",
            MemoryLevel.Session => $"session_{sessionId ?? "default"}",
            MemoryLevel.Agent => "agent_global",
            _ => "default"
        };
    }

    private List<MemoryRecallResult> ApplyLevelFiltering(
        List<MemoryRecallResult> results, 
        RecallOptions options)
    {
        // Filter by level preference
        if (options.PreferLevel.HasValue)
        {
            var preferred = results.Where(r => r.Level == options.PreferLevel.Value).ToList();
            var others = results.Where(r => r.Level != options.PreferLevel.Value).ToList();
            
            // Boost preferred level scores
            preferred = preferred.Select(r => r with { CombinedScore = r.CombinedScore * 1.2 }).ToList();
            
            results = preferred.Concat(others).ToList();
        }

        // Filter by user/session if specified
        if (options.UserId != null)
        {
            results = results.Where(r => 
                r.Metadata.TryGetValue("user_id", out var userId) && 
                userId == options.UserId).ToList();
        }

        if (options.SessionId != null)
        {
            results = results.Where(r => 
                r.Metadata.TryGetValue("session_id", out var sessionId) && 
                sessionId == options.SessionId).ToList();
        }

        return results;
    }
}

/// <summary>
/// Hybrid memory service interface.
/// </summary>
public interface IHybridMemoryService
{
    Task InitializeAsync(CancellationToken ct = default);
    
    Task<MemoryEntry> RememberAsync(
        string content,
        MemoryLevel level,
        MemoryOptions? options = null,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<MemoryRecallResult>> RecallAsync(
        string query,
        RecallOptions? options = null,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<MemoryEntry>> GetSessionMemoriesAsync(
        string sessionId,
        int limit = 100,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<MemoryEntry>> GetUserMemoriesAsync(
        string userId,
        int limit = 100,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<MemoryEntry>> GetRelatedMemoriesAsync(
        string memoryId,
        int minSharedEntities = 1,
        CancellationToken ct = default);
    
    Task ForgetAsync(string memoryId, CancellationToken ct = default);
    
    Task<HybridMemoryStatistics> GetStatisticsAsync(CancellationToken ct = default);
}

// DTOs
public sealed record MemoryEntry(
    string Id,
    string Content,
    MemoryLevel Level,
    string? UserId,
    string? SessionId,
    string? AgentId,
    DateTime CreatedAt,
    string Type,
    float Importance,
    IReadOnlyList<ExtractedEntity>? Entities = null,
    Dictionary<string, object>? Metadata = null);

public sealed record MemoryRecallResult(
    string Id,
    string Content,
    MemoryLevel Level,
    double SemanticScore,
    double GraphScore,
    double CombinedScore,
    string Source,
    DateTime CreatedAt,
    Dictionary<string, object> Metadata);

public sealed record MemoryOptions(
    string? UserId = null,
    string? SessionId = null,
    string? AgentId = null,
    string? Type = null,
    float Importance = 1.0f,
    Dictionary<string, object>? Metadata = null);

public sealed record RecallOptions(
    int TopK = 10,
    double MinSemanticScore = 0.7,
    int EntitySearchDepth = 2,
    MemoryLevel? PreferLevel = null,
    string? UserId = null,
    string? SessionId = null);

public sealed record HybridMemoryStatistics(
    int VectorRecords,
    int GraphNodes,
    int GraphRelationships,
    DateTime LastUpdated);
*/
