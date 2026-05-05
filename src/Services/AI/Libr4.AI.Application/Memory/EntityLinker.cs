using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Application.Memory;

/// <summary>
/// Service for entity linking across memories.
/// Links extracted entities to existing entities in knowledge graph.
/// Based on mem0's entity linking approach.
/// </summary>
public interface IEntityLinker
{
    /// <summary>
    /// Find or create links between extracted entities and existing graph entities.
    /// </summary>
    Task<IReadOnlyList<EntityLink>> LinkEntitiesAsync(
        string memoryId,
        IReadOnlyList<ExtractedEntity> entities,
        EntityLinkOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Merge duplicate entities based on similarity.
    /// </summary>
    Task<IReadOnlyList<EntityMerge>> FindMergesAsync(
        double minSimilarity = 0.85,
        CancellationToken ct = default);

    /// <summary>
    /// Get related entities through the graph.
    /// </summary>
    Task<IReadOnlyList<LinkedEntity>> GetRelatedEntitiesAsync(
        string entityId,
        int depth = 2,
        CancellationToken ct = default);
}

/// <summary>
/// Implementation using vector similarity + graph traversal.
/// </summary>
public sealed class EntityLinker : IEntityLinker
{
    private readonly IGraphMemoryStore _graphStore;
    private readonly IEmbeddingsService _embeddings;
    private readonly ILogger<EntityLinker> _logger;

    public EntityLinker(
        IGraphMemoryStore graphStore,
        IEmbeddingsService embeddings,
        ILogger<EntityLinker> logger)
    {
        _graphStore = graphStore;
        _embeddings = embeddings;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EntityLink>> LinkEntitiesAsync(
        string memoryId,
        IReadOnlyList<ExtractedEntity> entities,
        EntityLinkOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new EntityLinkOptions();
        var links = new List<EntityLink>();

        foreach (var entity in entities)
        {
            // Generate embedding for entity (temporarily disabled)
            var entityText = $"{entity.Name} {entity.Type} {entity.Description}";
            // var embedding = await _embeddings.GenerateEmbeddingAsync(entityText, cancellationToken: ct);
            var embedding = new float[1536];  // placeholder

            // Search for similar existing entities
            var candidates = await FindLinkCandidatesAsync(entity, embedding, options, ct);

            if (candidates.Count > 0 && candidates[0].Similarity >= options.MinSimilarityForMerge)
            {
                // Link to existing entity
                var bestMatch = candidates[0];
                links.Add(new EntityLink
                {
                    EntityId = entity.Id,
                    LinkedToEntityId = bestMatch.ExistingEntityId,
                    MemoryId = memoryId,
                    LinkType = EntityLinkType.SameAs,
                    Confidence = bestMatch.Similarity,
                    IsNew = false
                });

                _logger.LogDebug("Linked entity {Entity} to existing {Existing} (similarity: {Similarity:F2})",
                    entity.Name, bestMatch.ExistingEntityName, bestMatch.Similarity);
            }
            else
            {
                // Create new entity node
                links.Add(new EntityLink
                {
                    EntityId = entity.Id,
                    LinkedToEntityId = null,
                    MemoryId = memoryId,
                    LinkType = EntityLinkType.Mentions,
                    Confidence = 1.0,
                    IsNew = true
                });

                _logger.LogDebug("Created new entity node: {Entity}", entity.Name);
            }
        }

        return links;
    }

    public async Task<IReadOnlyList<EntityMerge>> FindMergesAsync(
        double minSimilarity = 0.85,
        CancellationToken ct = default)
    {
        // Get all entities from graph
        var allEntities = await _graphStore.GetAllEntitiesAsync(ct);
        var merges = new List<EntityMerge>();

        // Compare all pairs (O(n²) - optimize for production)
        for (int i = 0; i < allEntities.Count; i++)
        {
            for (int j = i + 1; j < allEntities.Count; j++)
            {
                var entity1 = allEntities[i];
                var entity2 = allEntities[j];

                // Skip if different types
                if (entity1.Type != entity2.Type) continue;

                var similarity = CalculateNameSimilarity(entity1.Name, entity2.Name);
                
                if (similarity >= minSimilarity)
                {
                    merges.Add(new EntityMerge
                    {
                        EntityId1 = entity1.Id,
                        EntityId2 = entity2.Id,
                        EntityName1 = entity1.Name,
                        EntityName2 = entity2.Name,
                        Similarity = similarity,
                        Reason = $"Name similarity: {similarity:F2}"
                    });
                }
            }
        }

        return merges.OrderByDescending(m => m.Similarity).ToList();
    }

    public async Task<IReadOnlyList<LinkedEntity>> GetRelatedEntitiesAsync(
        string entityId,
        int depth = 2,
        CancellationToken ct = default)
    {
        // Traverse graph to find related entities
        var query = depth switch
        {
            1 => $@"
                MATCH (e:Entity {{id: $entityId}})-[r]-(related:Entity)
                RETURN related, r.type as relationship_type, 1 as depth",
            _ => $@"
                MATCH path = (e:Entity {{id: $entityId}})-[*1..{depth}]-(related:Entity)
                WITH related, relationships(path) as rels, length(path) as d
                RETURN related, last(rels).type as relationship_type, d as depth"
        };

        var results = await _graphStore.QueryGraphAsync(query, new Dictionary<string, object>
        {
            ["entityId"] = entityId
        }, ct);

        return results.Select(r => new LinkedEntity
        {
            EntityId = r.Memory.Id, // Reusing MemoryNode structure
            EntityName = r.Memory.Content, // Entity name stored in content
            EntityType = r.Memory.Type,
            // RelationshipType = r.Metadata.GetValueOrDefault("relationship_type", "related"),
            // Depth = int.Parse(r.Metadata.GetValueOrDefault("depth", "1"))
            RelationshipType = "related",
            Depth = 1
        }).ToList();
    }

    private async Task<IReadOnlyList<LinkCandidate>> FindLinkCandidatesAsync(
        ExtractedEntity entity,
        float[] embedding,
        EntityLinkOptions options,
        CancellationToken ct)
    {
        // Search graph for entities of same type
        var candidates = new List<LinkCandidate>();
        
        // Query entities of same type
        var sameTypeEntities = await _graphStore.GetEntitiesByTypeAsync(entity.Type, ct);

        foreach (var existing in sameTypeEntities)
        {
            // Calculate embedding similarity
            var similarity = CosineSimilarity(embedding, existing.Embedding);
            
            // Boost if names are similar
            var nameSim = CalculateNameSimilarity(entity.Name, existing.Name);
            var combinedScore = (similarity * 0.7) + (nameSim * 0.3);

            if (combinedScore >= options.MinSimilarityForLink)
            {
                candidates.Add(new LinkCandidate
                {
                    ExistingEntityId = existing.Id,
                    ExistingEntityName = existing.Name,
                    Similarity = combinedScore
                });
            }
        }

        return candidates.OrderByDescending(c => c.Similarity).Take(5).ToList();
    }

    private static double CalculateNameSimilarity(string name1, string name2)
    {
        var n1 = name1.ToLowerInvariant();
        var n2 = name2.ToLowerInvariant();

        // Exact match
        if (n1 == n2) return 1.0;

        // Contains
        if (n1.Contains(n2) || n2.Contains(n1)) return 0.8;

        // Levenshtein distance ratio
        var distance = LevenshteinDistance(n1, n2);
        var maxLen = Math.Max(n1.Length, n2.Length);
        return 1.0 - (double)distance / maxLen;
    }

    private static int LevenshteinDistance(string s, string t)
    {
        int n = s.Length, m = t.Length;
        if (n == 0) return m;
        if (m == 0) return n;

        var d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

// Data models

public sealed class EntityLink
{
    public string EntityId { get; set; } = "";
    public string? LinkedToEntityId { get; set; }
    public string MemoryId { get; set; } = "";
    public EntityLinkType LinkType { get; set; }
    public double Confidence { get; set; }
    public bool IsNew { get; set; }
}

public enum EntityLinkType
{
    SameAs,      // Same entity
    Mentions,    // Memory mentions entity
    PartOf,      // Entity is part of another
    RelatedTo,   // Generic relation
    CreatedBy, // Entity created by another
    WorksFor     // Employment relation
}

public sealed class EntityLinkOptions
{
    public double MinSimilarityForLink { get; init; } = 0.75;
    public double MinSimilarityForMerge { get; init; } = 0.90;
    public bool AllowNewEntities { get; init; } = true;
    public int MaxCandidates { get; init; } = 5;
}

public sealed class LinkCandidate
{
    public string ExistingEntityId { get; set; } = "";
    public string ExistingEntityName { get; set; } = "";
    public double Similarity { get; set; }
}

public sealed class EntityMerge
{
    public string EntityId1 { get; set; } = "";
    public string EntityId2 { get; set; } = "";
    public string EntityName1 { get; set; } = "";
    public string EntityName2 { get; set; } = "";
    public double Similarity { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class LinkedEntity
{
    public string EntityId { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string RelationshipType { get; set; } = "";
    public int Depth { get; set; }
}

// Extensions to IGraphMemoryStore

public static class GraphMemoryStoreExtensions
{
    public static async Task<IReadOnlyList<EntityNode>> GetAllEntitiesAsync(
        this IGraphMemoryStore store, 
        CancellationToken ct)
    {
        var query = "MATCH (e:Entity) RETURN e";
        var results = await store.QueryGraphAsync(query, new Dictionary<string, object>(), ct);
        
        return results.Select(r => new EntityNode
        {
            Id = r.Memory.Id,
            Name = r.Memory.Content,
            Type = r.Memory.Type,
            Embedding = r.Memory.Metadata?.TryGetValue("embedding", out var emb) == true 
                ? JsonSerializer.Deserialize<float[]>(emb) 
                : Array.Empty<float>()
        }).ToList();
    }

    public static async Task<IReadOnlyList<EntityNode>> GetEntitiesByTypeAsync(
        this IGraphMemoryStore store,
        string type,
        CancellationToken ct)
    {
        var query = "MATCH (e:Entity {type: $type}) RETURN e";
        var results = await store.QueryGraphAsync(query, new Dictionary<string, object> { ["type"] = type }, ct);
        
        return results.Select(r => new EntityNode
        {
            Id = r.Memory.Id,
            Name = r.Memory.Content,
            Type = r.Memory.Type,
            Embedding = r.Memory.Metadata?.TryGetValue("embedding", out var emb) == true 
                ? JsonSerializer.Deserialize<float[]>(emb) 
                : Array.Empty<float>()
        }).ToList();
    }
}

public sealed class EntityNode
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public float[] Embedding { get; set; } = Array.Empty<float>();
}
