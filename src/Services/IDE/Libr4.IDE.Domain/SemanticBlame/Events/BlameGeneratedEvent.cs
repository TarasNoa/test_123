using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.SemanticBlame.Events;

/// <summary>
/// Domain event raised when blame is generated
/// </summary>
public class BlameGeneratedEvent : IDomainEvent
{
    public Guid SemanticBlameId { get; }
    public string BlameId { get; }
    public DateTime OccurredOn { get; }
    
    public BlameGeneratedEvent(
        Guid semanticBlameId,
        string blameId)
    {
        SemanticBlameId = semanticBlameId;
        BlameId = blameId;
        OccurredOn = DateTime.UtcNow;
    }
}
