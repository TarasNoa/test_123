namespace Libr4.IDE.Domain.SemanticCodeGraph;

/// <summary>
/// Entity representing relationships between code entities
/// </summary>
public class CodeRelationship
{
    public Guid Id { get; private set; }
    public Guid SourceEntityId { get; private set; }
    public Guid TargetEntityId { get; private set; }
    public string RelationshipType { get; private set; }
    public float Weight { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private CodeRelationship() { }
    
    public CodeRelationship(
        Guid sourceEntityId,
        Guid targetEntityId,
        string relationshipType,
        float weight = 1.0f)
    {
        Id = Guid.NewGuid();
        SourceEntityId = sourceEntityId;
        TargetEntityId = targetEntityId;
        RelationshipType = relationshipType;
        Weight = weight;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetWeight(float weight)
    {
        Weight = Math.Max(0.0f, Math.Min(1.0f, weight));
    }
    
    public static CodeRelationship Create(
        Guid sourceEntityId,
        Guid targetEntityId,
        string relationshipType,
        float weight = 1.0f)
    {
        return new CodeRelationship(sourceEntityId, targetEntityId, relationshipType, weight);
    }
}
