using System.ComponentModel.DataAnnotations;
using Libr4.IDE.Domain.FSharp;

namespace Libr4.IDE.Domain.Entities;

public class AgentEntity
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // For JWT Identity ownership
    public string OwnerId { get; set; } = string.Empty;
    
    // F# Agent State (serialized as text in DB)
    public AgentState State { get; set; } = StateMachine.idle();
    
    // Optimistic Concurrency
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
