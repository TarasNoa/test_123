using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.SemanticBlame.Events;

/// <summary>
/// Domain event raised when evolution is analyzed
/// </summary>
public class EvolutionAnalyzedEvent : IDomainEvent
{
    public Guid SemanticBlameId { get; }
    public string BlameId { get; }
    public DateTime OccurredOn { get; }
    
    public EvolutionAnalyzedEvent(
        Guid semanticBlameId,
        string blameId)
    {
        SemanticBlameId = semanticBlameId;
        BlameId = blameId;
        OccurredOn = DateTime.UtcNow;
    }
}
