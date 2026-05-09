using System.ComponentModel.DataAnnotations;

namespace Libr4.IDE.Domain.Entities;

public class AgentEntity
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // For JWT Identity ownership
    public string OwnerId { get; set; } = string.Empty;
    
    // Agent State (simplified, no F# dependency)
    public string State { get; set; } = "Idle";
    
    // Optimistic Concurrency
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
