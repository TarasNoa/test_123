using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.SemanticCodeGraph.Events;

/// <summary>
/// Domain event raised when a relationship is added to the graph
/// </summary>
public class RelationshipAddedEvent : IDomainEvent
{
    public Guid SemanticGraphId { get; }
    public string GraphId { get; }
    public string RelationshipType { get; }
    public DateTime OccurredOn { get; }
    
    public RelationshipAddedEvent(
        Guid semanticGraphId,
        string graphId,
        string relationshipType)
    {
        SemanticGraphId = semanticGraphId;
        GraphId = graphId;
        RelationshipType = relationshipType;
        OccurredOn = DateTime.UtcNow;
    }
}
