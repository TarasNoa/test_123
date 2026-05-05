using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.AgentMemorySystem.Events;

/// <summary>
/// Domain event raised when memory is compressed
/// </summary>
public class MemoryCompressedEvent : IDomainEvent
{
    public Guid AgentMemoryId { get; }
    public string MemoryId { get; }
    public MemoryCompressionLevel CompressionLevel { get; }
    public DateTime OccurredOn { get; }
    
    public MemoryCompressedEvent(
        Guid agentMemoryId,
        string memoryId,
        MemoryCompressionLevel compressionLevel)
    {
        AgentMemoryId = agentMemoryId;
        MemoryId = memoryId;
        CompressionLevel = compressionLevel;
        OccurredOn = DateTime.UtcNow;
    }
}
