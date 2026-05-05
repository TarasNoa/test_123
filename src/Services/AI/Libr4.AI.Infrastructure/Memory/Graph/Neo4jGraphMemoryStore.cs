/*
using System.Text.Json;
using Libr4.AI.Application.Memory;
using Neo4j.Driver;

namespace Libr4.AI.Infrastructure.Memory.Graph;

/// <summary>
/// Graph-based memory store using Neo4j.
/// Stores entities, relationships, and memories as nodes and edges.
/// Supports complex graph queries and path traversal.
/// </summary>
public sealed class Neo4jGraphMemoryStore : IGraphMemoryStore, IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jGraphMemoryStore> _logger;

    public Neo4jGraphMemoryStore(
        string uri,
        string username,
        string password,
        ILogger<Neo4jGraphMemoryStore> logger)
    {
        _logger = logger;
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
    }

    /// <summary>
    /// Initialize schema constraints and indexes.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await using var session = _driver.AsyncSession();
            
            // Create constraints for unique IDs
            var constraints = new[]
            {
                "CREATE CONSTRAINT memory_id IF NOT EXISTS FOR (m:Memory) REQUIRE m.id IS UNIQUE",
                "CREATE CONSTRAINT entity_id IF NOT EXISTS FOR (e:Entity) REQUIRE e.id IS UNIQUE",
                "CREATE CONSTRAINT memory_user_id IF NOT EXISTS FOR (m:Memory) REQUIRE m.userId IS UNIQUE",
                "CREATE CONSTRAINT entity_name IF NOT EXISTS FOR (e:Entity) REQUIRE e.name IS UNIQUE"
            };

            foreach (var constraint in constraints)
            {
                try
                {
                    await session.RunAsync(constraint, cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Constraint creation skipped (may already exist)");
                }
            }

            // Create indexes
            var indexes = new[]
            {
                "CREATE INDEX memory_type_idx IF NOT EXISTS FOR (m:Memory) ON (m.type)",
                "CREATE INDEX memory_created_idx IF NOT EXISTS FOR (m:Memory) ON (m.createdAt)",
                "CREATE INDEX entity_type_idx IF NOT EXISTS FOR (e:Entity) ON (e.type)",
                "CREATE INDEX relationship_type_idx IF NOT EXISTS FOR ()-[r:RELATES_TO]-() ON (r.type)"
            };

            foreach (var index in indexes)
            {
                try
                {
                    await session.RunAsync(index, cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Index creation skipped (may already exist)");
                }
            }

            _logger.LogInformation("Neo4j schema initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Neo4j schema");
            throw;
        }
    }

    /// <summary>
    /// Store a memory as a node with optional entity linking.
    /// </summary>
    public async Task StoreMemoryAsync(
        MemoryNode memory,
        IEnumerable<EntityLink>? entityLinks = null,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();
        
        await session.ExecuteWriteAsync(async tx =>
        {
            // Create memory node
            var memoryQuery = @"
                MERGE (m:Memory {id: $id})
                SET m.content = $content,
                    m.type = $type,
                    m.level = $level,
                    m.userId = $userId,
                    m.sessionId = $sessionId,
                    m.agentId = $agentId,
                    m.createdAt = $createdAt,
                    m.embeddingId = $embeddingId,
                    m.importance = $importance,
                    m.metadata = $metadata
                RETURN m";

            var memoryParams = new Dictionary<string, object>
            {
                ["id"] = memory.Id,
                ["content"] = memory.Content,
                ["type"] = memory.Type,
                ["level"] = memory.Level.ToString(),
                ["userId"] = memory.UserId ?? (object)NullValue.Instance,
                ["sessionId"] = memory.SessionId ?? (object)NullValue.Instance,
                ["agentId"] = memory.AgentId ?? (object)NullValue.Instance,
                ["createdAt"] = memory.CreatedAt.ToString("O"),
                ["embeddingId"] = memory.EmbeddingId ?? (object)NullValue.Instance,
                ["importance"] = memory.Importance,
                ["metadata"] = JsonSerializer.Serialize(memory.Metadata ?? new Dictionary<string, string>())
            };

            await tx.RunAsync(memoryQuery, memoryParams);

            // Create entity nodes and relationships
            if (entityLinks != null)
            {
                foreach (var link in entityLinks)
                {
                    var entityQuery = @"
                        MERGE (e:Entity {id: $entityId})
                        SET e.name = $entityName,
                            e.type = $entityType
                        WITH e
                        MATCH (m:Memory {id: $memoryId})
                        MERGE (m)-[r:MENTIONS {type: $relationshipType, weight: $weight, createdAt: $createdAt}]->(e)
                        RETURN e";

                    var entityParams = new Dictionary<string, object>
                    {
                        ["entityId"] = link.EntityId,
                        ["entityName"] = link.EntityName,
                        ["entityType"] = link.EntityType,
                        ["memoryId"] = memory.Id,
                        ["relationshipType"] = link.RelationshipType,
                        ["weight"] = link.Weight,
                        ["createdAt"] = DateTime.UtcNow.ToString("O")
                    };

                    await tx.RunAsync(entityQuery, entityParams);
                }
            }
        }, cancellationToken: ct);

        _logger.LogDebug("Stored memory {MemoryId} with {EntityCount} entities", 
            memory.Id, entityLinks?.Count() ?? 0);
    }

    /// <summary>
    /// Retrieve memories by semantic similarity through vector ID lookup.
    /// </summary>
    public async Task<IReadOnlyList<MemoryNode>> GetMemoriesByEmbeddingIdAsync(
        string embeddingId,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();
        
        var result = await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
                MATCH (m:Memory)
                WHERE m.embeddingId = $embeddingId
                RETURN m
                ORDER BY m.createdAt DESC";

            var cursor = await tx.RunAsync(query, new { embeddingId });
            var records = await cursor.ToListAsync();
            
            return records.Select(r => MapToMemoryNode(r["m"].As<INode>())).ToList();
        }, cancellationToken: ct);

        return result;
    }

    /// <summary>
    /// Search memories by entity traversal.
    /// </summary>
    public async Task<IReadOnlyList<MemoryNode>> SearchByEntityPathAsync(
        string startEntityId,
        int depth = 2,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();
        
        var result = await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
                MATCH path = (e:Entity {id: $startEntityId})<-[:MENTIONS*1.." + depth + @"]-(m:Memory)
                WITH m, length(path) as distance
                ORDER BY distance ASC, m.importance DESC
                RETURN DISTINCT m
                LIMIT 50";

            var cursor = await tx.RunAsync(query, new { startEntityId });
            var records = await cursor.ToListAsync();
            
            return records.Select(r => MapToMemoryNode(r["m"].As<INode>())).ToList();
        }, cancellationToken: ct);

        return result;
    }

    /// <summary>
    /// Find related memories through shared entities.
    /// </summary>
    public async Task<IReadOnlyList<MemoryNode>> GetRelatedMemoriesAsync(
        string memoryId,
        int minSharedEntities = 1,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();
        
        var result = await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
                MATCH (m:Memory {id: $memoryId})-[:MENTIONS]->(e:Entity)<-[:MENTIONS]-(related:Memory)
                WHERE related.id <> $memoryId
                WITH related, count(e) as sharedEntities
                WHERE sharedEntities >= $minShared
                RETURN related, sharedEntities
                ORDER BY sharedEntities DESC, related.importance DESC
                LIMIT 20";

            var cursor = await tx.RunAsync(query, new { memoryId, minShared = minSharedEntities });
            var records = await cursor.ToListAsync();
            
            return records.Select(r => MapToMemoryNode(r["related"].As<INode>())).ToList();
        }, cancellationToken: ct);

        return result;
    }

    /// <summary>
    /// Query memories with complex graph patterns.
    /// </summary>
    public async Task<IReadOnlyList<MemoryQueryResult>> QueryGraphAsync(
        string cypherQuery,
        Dictionary<string, object>? parameters = null,
        CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();
        
        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(cypherQuery, parameters ?? new Dictionary<string, object>());
            var records = await cursor.ToListAsync();
            
            return records.Select(r => new MemoryQueryResult(
                Memory: MapToMemoryNode(r.GetValue<INode>("m")),
                Path: r.TryGetValue<IPath>("path", out var path) ? path : null,
                Score: r.TryGetValue<double>("score", out var score) ? score : 0
            )).ToList();
        }, cancellationToken: ct);

        return result;
    }

    /// <summary>
    /// Delete a memory and its relationships.
    /// </summary>
    public async Task DeleteMemoryAsync(string memoryId, CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();
        
        await session.ExecuteWriteAsync(async tx =>
        {
            var query = @"
                MATCH (m:Memory {id: $memoryId})
                OPTIONAL MATCH (m)-[r]-()
                DELETE r, m";

            await tx.RunAsync(query, new { memoryId });
        }, cancellationToken: ct);

        _logger.LogDebug("Deleted memory {MemoryId}", memoryId);
    }

    /// <summary>
    /// Get statistics about the graph.
    /// </summary>
    public async Task<GraphStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        await using var session = _driver.AsyncSession();
        
        var result = await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
                MATCH (m:Memory)
                WITH count(m) as memoryCount
                MATCH (e:Entity)
                WITH memoryCount, count(e) as entityCount
                MATCH ()-[r:MENTIONS]->()
                RETURN memoryCount, entityCount, count(r) as relationshipCount";

            var cursor = await tx.RunAsync(query);
            var record = await cursor.SingleAsync();

            return new GraphStatistics(
                MemoryNodeCount: record["memoryCount"].As<int>(),
                EntityNodeCount: record["entityCount"].As<int>(),
                RelationshipCount: record["relationshipCount"].As<int>(),
                LastUpdated: DateTime.UtcNow
            );
        }, cancellationToken: ct);

        return result;
    }

    private MemoryNode MapToMemoryNode(INode node)
    {
        return new MemoryNode(
            Id: node["id"].As<string>(),
            Content: node["content"].As<string>(),
            Type: node["type"].As<string>(),
            Level: Enum.Parse<MemoryLevel>(node["level"].As<string>()),
            UserId: node.TryGetValue<string>("userId", out var userId) ? userId : null,
            SessionId: node.TryGetValue<string>("sessionId", out var sessionId) ? sessionId : null,
            AgentId: node.TryGetValue<string>("agentId", out var agentId) ? agentId : null,
            CreatedAt: DateTime.Parse(node["createdAt"].As<string>()),
            EmbeddingId: node.TryGetValue<string>("embeddingId", out var embId) ? embId : null,
            Importance: node.TryGetValue<float>("importance", out var importance) ? importance : 1.0f,
            Metadata: node.TryGetValue<string>("metadata", out var metaStr) 
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(metaStr) 
                : null
        );
    }

    public async ValueTask DisposeAsync()
    {
        _driver?.DisposeAsync();
    }
}

/// <summary>
/// Configuration for Neo4j connection.
/// </summary>
public sealed record Neo4jOptions
{
    public string Uri { get; init; } = "bolt://localhost:7687";
    public string Username { get; init; } = "neo4j";
    public string Password { get; init; } = string.Empty;
    public int MaxConnectionPoolSize { get; init; } = 20;
}

/// <summary>
/// Statistics about the graph database.
/// </summary>
public sealed record GraphStatistics(
    int MemoryNodeCount,
    int EntityNodeCount,
    int RelationshipCount,
    DateTime LastUpdated);
*/
