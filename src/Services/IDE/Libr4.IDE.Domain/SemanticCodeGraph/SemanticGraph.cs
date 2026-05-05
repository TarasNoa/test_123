using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.SemanticCodeGraph.Events;

namespace Libr4.IDE.Domain.SemanticCodeGraph;

/// <summary>
/// AggregateRoot for semantic graph
/// </summary>
public class SemanticGraph : AggregateRoot<Guid>
{
    public string GraphId { get; private set; }
    public string WorkspaceId { get; private set; }
    public List<CodeEntity> Entities { get; private set; }
    public List<CodeRelationship> Relationships { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private SemanticGraph() { }
    
    public SemanticGraph(
        string graphId,
        string workspaceId,
        List<CodeEntity>? entities = null,
        List<CodeRelationship>? relationships = null)
    {
        Id = Guid.NewGuid();
        GraphId = graphId;
        WorkspaceId = workspaceId;
        Entities = entities ?? new List<CodeEntity>();
        Relationships = relationships ?? new List<CodeRelationship>();
        Status = "initializing";
        CreatedAt = DateTime.UtcNow;
        CompletedAt = null;
    }
    
    public void AddEntity(CodeEntity entity)
    {
        if (entity != null)
        {
            Entities.Add(entity);
        }
    }
    
    public void AddRelationship(CodeRelationship relationship)
    {
        if (relationship != null)
        {
            Relationships.Add(relationship);
        }
    }
    
    public void SetStatus(string status)
    {
        Status = status;
        if (status == "completed" || status == "failed")
        {
            CompletedAt = DateTime.UtcNow;
        }
    }
    
    public CodeEntity? GetEntityById(Guid entityId)
    {
        return Entities.FirstOrDefault(e => e.Id == entityId);
    }
    
    /// <summary>
    /// Marks the graph as created and raises a domain event
    /// </summary>
    public void MarkAsCreated()
    {
        AddDomainEvent(new GraphCreatedEvent(Id, GraphId));
    }
    
    /// <summary>
    /// Marks an entity as added and raises a domain event
    /// </summary>
    public void MarkEntityAdded(CodeEntity entity)
    {
        AddDomainEvent(new EntityAddedEvent(Id, GraphId, entity.Id, entity.EntityType));
    }
    
    /// <summary>
    /// Marks a relationship as added and raises a domain event
    /// </summary>
    public void MarkRelationshipAdded(CodeRelationship relationship)
    {
        AddDomainEvent(new RelationshipAddedEvent(Id, GraphId, relationship.RelationshipType));
    }
    
    public static SemanticGraph Create(
        string graphId,
        string workspaceId,
        List<CodeEntity>? entities = null,
        List<CodeRelationship>? relationships = null)
    {
        return new SemanticGraph(graphId, workspaceId, entities, relationships);
    }
}
