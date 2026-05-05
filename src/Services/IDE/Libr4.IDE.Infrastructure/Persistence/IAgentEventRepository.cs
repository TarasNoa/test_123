using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;

namespace Libr4.IDE.Infrastructure.Persistence;

public interface IAgentEventRepository
{
    Task SaveAsync(AgentEvent evt, CancellationToken ct = default);
    Task<AgentEvent[]> GetEventsForRunAsync(Guid runId, CancellationToken ct = default);
    Task ClearEventsForRunAsync(Guid runId, CancellationToken ct = default);
}
