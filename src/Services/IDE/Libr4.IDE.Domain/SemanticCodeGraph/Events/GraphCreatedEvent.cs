using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.SemanticCodeGraph.Events;

/// <summary>
/// Domain event raised when a semantic graph is created
/// </summary>
public class GraphCreatedEvent : IDomainEvent
{
    public Guid SemanticGraphId { get; }
    public string GraphId { get; }
    public DateTime OccurredOn { get; }
    
    public GraphCreatedEvent(
        Guid semanticGraphId,
        string graphId)
    {
        SemanticGraphId = semanticGraphId;
        GraphId = graphId;
        OccurredOn = DateTime.UtcNow;
    }
}
