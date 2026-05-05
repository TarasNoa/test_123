using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.AgentMemorySystem.Events;

/// <summary>
/// Domain event raised when agent memory is created
/// </summary>
public class MemoryCreatedEvent : IDomainEvent
{
    public Guid AgentMemoryId { get; }
    public string MemoryId { get; }
    public DateTime OccurredOn { get; }
    
    public MemoryCreatedEvent(
        Guid agentMemoryId,
        string memoryId)
    {
        AgentMemoryId = agentMemoryId;
        MemoryId = memoryId;
        OccurredOn = DateTime.UtcNow;
    }
}
