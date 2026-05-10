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

public interface IAgentService
{
    Task<List<AgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default);
    Task<AgentDto?> GetAgentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AgentDto> CreateAgentAsync(CreateAgentRequest request, CancellationToken cancellationToken = default);
    Task ActivateAgentAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAgentAsync(Guid id, CancellationToken cancellationToken = default);
}