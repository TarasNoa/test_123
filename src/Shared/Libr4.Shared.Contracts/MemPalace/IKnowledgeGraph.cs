namespace Libr4.Shared.Contracts.MemPalace;

/// <summary>
/// Represents an entity in the knowledge graph.
/// </summary>
public record GraphEntity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Entity name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Entity type (person, project, concept, etc.).
    /// </summary>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>
    /// Entity properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; init; } = new();

    /// <summary>
    /// When the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Represents a relationship between entities.
/// </summary>
public record GraphRelationship
{
    /// <summary>
    /// Unique identifier for the relationship.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Source entity ID.
    /// </summary>
    public string FromEntityId { get; init; } = string.Empty;

    /// <summary>
    /// Target entity ID.
    /// </summary>
    public string ToEntityId { get; init; } = string.Empty;

    /// <summary>
    /// Relationship type (e.g., "works_on", "related_to", "depends_on").
    /// </summary>
    public string RelationshipType { get; init; } = string.Empty;

    /// <summary>
    /// Validity window start.
    /// </summary>
    public DateTime ValidFrom { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Validity window end (null if still valid).
    /// </summary>
    public DateTime? ValidTo { get; init; }

    /// <summary>
    /// Relationship properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; init; } = new();

    /// <summary>
    /// When the relationship was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a timeline event for temporal tracking.
/// </summary>
public record TimelineEvent
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Event type.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Entity ID associated with the event.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Relationship ID associated with the event.
    /// </summary>
    public string? RelationshipId { get; init; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Event data.
    /// </summary>
    public Dictionary<string, string> Data { get; init; } = new();

    /// <summary>
    /// Event description.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Interface for knowledge graph service.
/// </summary>
public interface IKnowledgeGraphService
{
    /// <summary>
    /// Adds an entity to the graph.
    /// </summary>
    /// <param name="entity">Entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added entity.</returns>
    Task<GraphEntity> AddEntityAsync(
        GraphEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an entity by ID.
    /// </summary>
    /// <param name="entityId">Entity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity, or null if not found.</returns>
    Task<GraphEntity?> GetEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities by type.
    /// </summary>
    /// <param name="entityType">Entity type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of entities of the type.</returns>
    Task<IReadOnlyList<GraphEntity>> GetEntitiesByTypeAsync(
        string entityType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity.
    /// </summary>
    /// <param name="entity">Entity to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity.</returns>
    Task<GraphEntity> UpdateEntityAsync(
        GraphEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a relationship between entities.
    /// </summary>
    /// <param name="relationship">Relationship to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added relationship.</returns>
    Task<GraphRelationship> AddRelationshipAsync(
        GraphRelationship relationship,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets relationships for an entity.
    /// </summary>
    /// <param name="entityId">Entity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of relationships for the entity.</returns>
    Task<IReadOnlyList<GraphRelationship>> GetRelationshipsAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates a relationship (sets ValidTo to current time).
    /// </summary>
    /// <param name="relationshipId">Relationship ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The invalidated relationship.</returns>
    Task<GraphRelationship> InvalidateRelationshipAsync(
        string relationshipId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the graph for entities matching criteria.
    /// </summary>
    /// <param name="query">Graph query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching entities.</returns>
    Task<IReadOnlyList<GraphEntity>> QueryAsync(
        GraphQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a timeline event.
    /// </summary>
    /// <param name="event">Timeline event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added event.</returns>
    Task<TimelineEvent> AddTimelineEventAsync(
        TimelineEvent @event,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets timeline events for an entity.
    /// </summary>
    /// <param name="entityId">Entity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of timeline events.</returns>
    Task<IReadOnlyList<TimelineEvent>> GetTimelineEventsAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets timeline events within a time range.
    /// </summary>
    /// <param name="from">Start time.</param>
    /// <param name="to">End time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of timeline events.</returns>
    Task<IReadOnlyList<TimelineEvent>> GetTimelineEventsInRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Query for the knowledge graph.
/// </summary>
public record GraphQuery
{
    /// <summary>
    /// Entity type to filter by.
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Property filters.
    /// </summary>
    public Dictionary<string, string> PropertyFilters { get; init; } = new();

    /// <summary>
    /// Relationship type to traverse.
    /// </summary>
    public string? TraverseRelationshipType { get; init; }

    /// <summary>
    /// Maximum depth for traversal.
    /// </summary>
    public int MaxDepth { get; init; } = 1;

    /// <summary>
    /// Whether to include related entities.
    /// </summary>
    public bool IncludeRelated { get; init; } = false;
}

/// <summary>
/// In-memory implementation of knowledge graph service.
/// </summary>
public class InMemoryKnowledgeGraphService : IKnowledgeGraphService
{
    private readonly Dictionary<string, GraphEntity> _entities = new();
    private readonly Dictionary<string, GraphRelationship> _relationships = new();
    private readonly List<TimelineEvent> _timelineEvents = new();

    public Task<GraphEntity> AddEntityAsync(
        GraphEntity entity,
        CancellationToken cancellationToken = default)
    {
        _entities[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task<GraphEntity?> GetEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        _entities.TryGetValue(entityId, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<GraphEntity>> GetEntitiesByTypeAsync(
        string entityType,
        CancellationToken cancellationToken = default)
    {
        var entities = _entities.Values
            .Where(e => e.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult<IReadOnlyList<GraphEntity>>(entities);
    }

    public Task<GraphEntity> UpdateEntityAsync(
        GraphEntity entity,
        CancellationToken cancellationToken = default)
    {
        if (!_entities.ContainsKey(entity.Id))
        {
            throw new ArgumentException($"Entity with ID {entity.Id} not found", nameof(entity));
        }

        var updated = entity with { UpdatedAt = DateTime.UtcNow };
        _entities[entity.Id] = updated;
        return Task.FromResult(updated);
    }

    public Task<GraphRelationship> AddRelationshipAsync(
        GraphRelationship relationship,
        CancellationToken cancellationToken = default)
    {
        if (!_entities.ContainsKey(relationship.FromEntityId))
        {
            throw new ArgumentException($"From entity with ID {relationship.FromEntityId} not found", nameof(relationship));
        }

        if (!_entities.ContainsKey(relationship.ToEntityId))
        {
            throw new ArgumentException($"To entity with ID {relationship.ToEntityId} not found", nameof(relationship));
        }

        _relationships[relationship.Id] = relationship;
        return Task.FromResult(relationship);
    }

    public Task<IReadOnlyList<GraphRelationship>> GetRelationshipsAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        var relationships = _relationships.Values
            .Where(r => r.FromEntityId == entityId || r.ToEntityId == entityId)
            .Where(r => r.ValidTo == null || r.ValidTo > DateTime.UtcNow)
            .ToList();

        return Task.FromResult<IReadOnlyList<GraphRelationship>>(relationships);
    }

    public Task<GraphRelationship> InvalidateRelationshipAsync(
        string relationshipId,
        CancellationToken cancellationToken = default)
    {
        if (!_relationships.TryGetValue(relationshipId, out var relationship))
        {
            throw new ArgumentException($"Relationship with ID {relationshipId} not found", nameof(relationshipId));
        }

        var invalidated = relationship with { ValidTo = DateTime.UtcNow };
        _relationships[relationshipId] = invalidated;
        return Task.FromResult(invalidated);
    }

    public async Task<IReadOnlyList<GraphEntity>> QueryAsync(
        GraphQuery query,
        CancellationToken cancellationToken = default)
    {
        var results = _entities.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(query.EntityType))
        {
            results = results.Where(e => e.EntityType.Equals(query.EntityType, StringComparison.OrdinalIgnoreCase));
        }

        if (query.PropertyFilters.Any())
        {
            results = results.Where(e => query.PropertyFilters.All(kvp =>
                e.Properties.TryGetValue(kvp.Key, out var value) && value.Equals(kvp.Value, StringComparison.OrdinalIgnoreCase)));
        }

        if (query.IncludeRelated && !string.IsNullOrEmpty(query.TraverseRelationshipType))
        {
            var relatedEntityIds = new HashSet<string>();
            foreach (var entity in results)
            {
                var relationships = await GetRelationshipsAsync(entity.Id, cancellationToken);
                foreach (var rel in relationships.Where(r => r.RelationshipType.Equals(query.TraverseRelationshipType, StringComparison.OrdinalIgnoreCase)))
                {
                    relatedEntityIds.Add(rel.ToEntityId);
                    relatedEntityIds.Add(rel.FromEntityId);
                }
            }

            var relatedEntities = relatedEntityIds
                .Where(id => _entities.ContainsKey(id))
                .Select(id => _entities[id]);

            results = results.Concat(relatedEntities);
        }

        return results.ToList().AsReadOnly();
    }

    public Task<TimelineEvent> AddTimelineEventAsync(
        TimelineEvent @event,
        CancellationToken cancellationToken = default)
    {
        _timelineEvents.Add(@event);
        return Task.FromResult(@event);
    }

    public Task<IReadOnlyList<TimelineEvent>> GetTimelineEventsAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        var events = _timelineEvents
            .Where(e => e.EntityId == entityId)
            .OrderBy(e => e.Timestamp)
            .ToList();

        return Task.FromResult<IReadOnlyList<TimelineEvent>>(events);
    }

    public Task<IReadOnlyList<TimelineEvent>> GetTimelineEventsInRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var events = _timelineEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .OrderBy(e => e.Timestamp)
            .ToList();

        return Task.FromResult<IReadOnlyList<TimelineEvent>>(events);
    }
}
