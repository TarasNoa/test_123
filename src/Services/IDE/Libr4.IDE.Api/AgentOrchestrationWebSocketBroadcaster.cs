using Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;

namespace Libr4.IDE.Api;

public sealed class AgentOrchestrationWebSocketBroadcaster : IAgentOrchestrationBroadcaster
{
    private readonly AgentEventWebSocketHandler _handler;

    public AgentOrchestrationWebSocketBroadcaster(AgentEventWebSocketHandler handler) =>
        _handler = handler;

    public Task PublishAsync(AgentOrchestrationBroadcast broadcast, CancellationToken ct = default)
    {
        if (broadcast.Orchestration is null)
            return Task.CompletedTask;

        return _handler.BroadcastOrchestrationAsync(broadcast.Orchestration);
    }
}
