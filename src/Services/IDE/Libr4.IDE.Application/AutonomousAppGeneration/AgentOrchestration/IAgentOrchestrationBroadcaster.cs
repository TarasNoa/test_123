namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;

public sealed record AgentOrchestrationBroadcast(
    string Kind,
    Guid RunId,
    AgentOrchestrationEvent? Orchestration,
    Guid? AgentId = null,
    string? Error = null);

public interface IAgentOrchestrationBroadcaster
{
    Task PublishAsync(AgentOrchestrationBroadcast broadcast, CancellationToken ct = default);
}

public sealed class NullAgentOrchestrationBroadcaster : IAgentOrchestrationBroadcaster
{
    public Task PublishAsync(AgentOrchestrationBroadcast broadcast, CancellationToken ct = default) =>
        Task.CompletedTask;
}
