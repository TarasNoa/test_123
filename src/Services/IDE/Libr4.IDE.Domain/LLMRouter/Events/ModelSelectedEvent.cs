using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.LLMRouter.Events;

/// <summary>
/// Domain event raised when a model is selected
/// </summary>
public class ModelSelectedEvent : IDomainEvent
{
    public Guid LLMRoutingId { get; }
    public string RoutingId { get; }
    public string ModelName { get; }
    public DateTime OccurredOn { get; }
    
    public ModelSelectedEvent(
        Guid llmRoutingId,
        string routingId,
        string modelName)
    {
        LLMRoutingId = llmRoutingId;
        RoutingId = routingId;
        ModelName = modelName;
        OccurredOn = DateTime.UtcNow;
    }
}
