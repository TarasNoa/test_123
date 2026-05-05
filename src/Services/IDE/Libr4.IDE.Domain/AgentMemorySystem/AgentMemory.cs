using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.AgentMemorySystem.Events;

namespace Libr4.IDE.Domain.AgentMemorySystem;

/// <summary>
/// AggregateRoot for agent memory
/// </summary>
public class AgentMemory : AggregateRoot<Guid>
{
    public string MemoryId { get; private set; }
    public string AgentId { get; private set; }
    public List<MemoryFragment> Fragments { get; private set; }
    public MemoryCompressionLevel CompressionLevel { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastCompressedAt { get; private set; }
    
    private AgentMemory() { }
    
    public AgentMemory(
        string memoryId,
        string agentId,
        MemoryCompressionLevel compressionLevel = MemoryCompressionLevel.Medium,
        List<MemoryFragment>? fragments = null)
    {
        Id = Guid.NewGuid();
        MemoryId = memoryId;
        AgentId = agentId;
        Fragments = fragments ?? new List<MemoryFragment>();
        CompressionLevel = compressionLevel;
        Status = "initializing";
        CreatedAt = DateTime.UtcNow;
        LastCompressedAt = null;
    }
    
    public void AddFragment(MemoryFragment fragment)
    {
        if (fragment != null)
        {
            Fragments.Add(fragment);
        }
    }
    
    public void SetCompressionLevel(MemoryCompressionLevel level)
    {
        CompressionLevel = level;
    }
    
    public void SetStatus(string status)
    {
        Status = status;
    }

    public List<MemoryFragment> GetActiveFragments()
    {
        return Fragments.Where(f => !f.IsExpired()).ToList();
    }
    
    /// <summary>
    /// Marks the memory as created and raises a domain event
    /// </summary>
    public void MarkAsCreated()
    {
        AddDomainEvent(new MemoryCreatedEvent(Id, MemoryId));
    }
    
    /// <summary>
    /// Marks a fragment as added and raises a domain event
    /// </summary>
    public void MarkFragmentAdded(MemoryFragment fragment)
    {
        AddDomainEvent(new FragmentAddedEvent(Id, MemoryId, fragment.Id));
    }
    
    /// <summary>
    /// Marks memory as compressed and raises a domain event
    /// </summary>
    public void MarkCompressed()
    {
        LastCompressedAt = DateTime.UtcNow;
        AddDomainEvent(new MemoryCompressedEvent(Id, MemoryId, CompressionLevel));
    }
    
    public static AgentMemory Create(
        string memoryId,
        string agentId,
        MemoryCompressionLevel compressionLevel = MemoryCompressionLevel.Medium,
        List<MemoryFragment>? fragments = null)
    {
        return new AgentMemory(memoryId, agentId, compressionLevel, fragments);
    }
}
