namespace Libr4.AI.Application.Memory;

/// <summary>
/// Extracts entities from text for graph database storage.
/// Identifies people, organizations, concepts, and their relationships.
/// </summary>
public interface IEntityExtractor
{
    /// <summary>
    /// Extract all entities from a text.
    /// </summary>
    Task<IReadOnlyList<ExtractedEntity>> ExtractEntitiesAsync(
        string text,
        ExtractionOptions? options = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Extract entities and their relationships from text.
    /// </summary>
    Task<ExtractionResult> ExtractWithRelationshipsAsync(
        string text,
        ExtractionOptions? options = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Link extracted entities to existing entities in the knowledge base.
    /// </summary>
    Task<IReadOnlyList<EntityLinkCandidate>> LinkEntitiesAsync(
        IReadOnlyList<ExtractedEntity> entities,
        string userId,
        CancellationToken ct = default);
}

/// <summary>
/// An entity extracted from text.
/// </summary>
public sealed record ExtractedEntity(
    string Id,
    string Name,
    string Type, // Person, Organization, Concept, Location, etc.
    string? Description = null,
    float Confidence = 1.0f,
    IReadOnlyList<EntityMention>? Mentions = null);

/// <summary>
/// A mention of an entity in the source text.
/// </summary>
public sealed record EntityMention(
    int StartIndex,
    int EndIndex,
    string Context);

/// <summary>
/// A relationship between two entities.
/// </summary>
public sealed record EntityRelationship(
    string SourceEntityId,
    string TargetEntityId,
    string RelationshipType,
    float Confidence = 1.0f);

/// <summary>
/// Complete extraction result with entities and relationships.
/// </summary>
public sealed record ExtractionResult(
    IReadOnlyList<ExtractedEntity> Entities,
    IReadOnlyList<EntityRelationship> Relationships);

/// <summary>
/// Candidate for entity linking to existing knowledge base.
/// </summary>
public sealed record EntityLinkCandidate(
    ExtractedEntity NewEntity,
    string? ExistingEntityId,
    string? ExistingEntityName,
    float SimilarityScore,
    bool ShouldLink);

/// <summary>
/// Options for entity extraction.
/// </summary>
public sealed record ExtractionOptions
{
    /// <summary>
    /// Minimum confidence score for entity extraction (0.0 - 1.0).
    /// </summary>
    public float MinConfidence { get; init; } = 0.7f;
    
    /// <summary>
    /// Entity types to extract. Null means all types.
    /// </summary>
    public IReadOnlyList<string>? EntityTypes { get; init; }
    
    /// <summary>
    /// Whether to extract relationships between entities.
    /// </summary>
    public bool ExtractRelationships { get; init; } = true;
    
    /// <summary>
    /// User context for entity linking.
    /// </summary>
    public string? UserId { get; init; }
}
