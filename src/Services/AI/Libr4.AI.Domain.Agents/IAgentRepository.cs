using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Libr4.AI.Domain.Agents;

public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Agent>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Agent agent, CancellationToken cancellationToken = default);
    Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}