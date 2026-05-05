using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.SemanticBlame.Events;

/// <summary>
/// Domain event raised when blame is completed
/// </summary>
public class BlameCompletedEvent : IDomainEvent
{
    public Guid SemanticBlameId { get; }
    public string BlameId { get; }
    public int EntriesCount { get; }
    public DateTime OccurredOn { get; }
    
    public BlameCompletedEvent(
        Guid semanticBlameId,
        string blameId,
        int entriesCount)
    {
        SemanticBlameId = semanticBlameId;
        BlameId = blameId;
        EntriesCount = entriesCount;
        OccurredOn = DateTime.UtcNow;
    }
}
