using Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;

namespace Libr4.IDE.Infrastructure.Persistence;

public interface IAgentOrchestrationRepository
{
    Task SaveAsync(AgentOrchestrationEvent evt, CancellationToken ct = default);
    Task<AgentOrchestrationEvent?> GetOrchestrationAsync(Guid runId, CancellationToken ct = default);
    Task ClearOrchestrationAsync(Guid runId, CancellationToken ct = default);
}
