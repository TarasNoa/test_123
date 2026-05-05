using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.AgentMemorySystem.Events;

/// <summary>
/// Domain event raised when a memory fragment is added
/// </summary>
public class FragmentAddedEvent : IDomainEvent
{
    public Guid AgentMemoryId { get; }
    public string MemoryId { get; }
    public Guid FragmentId { get; }
    public DateTime OccurredOn { get; }
    
    public FragmentAddedEvent(
        Guid agentMemoryId,
        string memoryId,
        Guid fragmentId)
    {
        AgentMemoryId = agentMemoryId;
        MemoryId = memoryId;
        FragmentId = fragmentId;
        OccurredOn = DateTime.UtcNow;
    }
}
