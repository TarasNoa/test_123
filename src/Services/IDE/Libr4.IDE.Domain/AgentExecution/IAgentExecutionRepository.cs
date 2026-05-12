namespace Libr4.IDE.Domain.AgentExecution;

public interface IAgentExecutionRepository
{
    Task<AgentExecutionContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AgentExecutionContext context, CancellationToken cancellationToken = default);
    Task UpdateAsync(AgentExecutionContext context, CancellationToken cancellationToken = default);
}
