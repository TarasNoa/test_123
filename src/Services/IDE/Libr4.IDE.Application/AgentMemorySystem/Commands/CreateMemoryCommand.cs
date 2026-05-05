using MediatR;
using Libr4.IDE.Domain.AgentMemorySystem;
using Libr4.IDE.Application.AgentMemorySystem.DTOs;

namespace Libr4.IDE.Application.AgentMemorySystem.Commands;

/// <summary>
/// Command to create agent memory
/// </summary>
public record CreateMemoryCommand : IRequest<AgentMemoryDto>
{
    public string AgentId { get; init; } = string.Empty;
    public MemoryCompressionLevel CompressionLevel { get; init; } = MemoryCompressionLevel.Medium;
}
