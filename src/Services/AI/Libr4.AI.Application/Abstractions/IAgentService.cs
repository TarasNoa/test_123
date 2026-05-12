using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Libr4.AI.Application.Abstractions;

public record AgentDto(
    Guid Id,
    string Name,
    string Role,
    string Status,
    DateTimeOffset CreatedAt);

public class CreateAgentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public interface IAgentService
{
    Task<List<AgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default);
    Task<AgentDto?> GetAgentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AgentDto> CreateAgentAsync(CreateAgentRequest request, CancellationToken cancellationToken = default);
    Task ActivateAgentAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAgentAsync(Guid id, CancellationToken cancellationToken = default);
}