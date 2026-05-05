using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.SemanticCodeGraph.Events;

/// <summary>
/// Domain event raised when an entity is added to the graph
/// </summary>
public class EntityAddedEvent : IDomainEvent
{
    public Guid SemanticGraphId { get; }
    public string GraphId { get; }
    public Guid EntityId { get; }
    public string EntityType { get; }
    public DateTime OccurredOn { get; }
    
    public EntityAddedEvent(
        Guid semanticGraphId,
        string graphId,
        Guid entityId,
        string entityType)
    {
        SemanticGraphId = semanticGraphId;
        GraphId = graphId;
        EntityId = entityId;
        EntityType = entityType;
        OccurredOn = DateTime.UtcNow;
    }
}
