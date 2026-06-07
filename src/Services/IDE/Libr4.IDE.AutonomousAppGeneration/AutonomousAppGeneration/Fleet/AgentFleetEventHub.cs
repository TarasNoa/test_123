namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface IAgentFleetEventHub
{
    event Func<AgentFleetStatusEvent, Task>? EventPublished;
    Task PublishAsync(AgentFleetStatusEvent evt, CancellationToken ct = default);
}

public sealed class AgentFleetEventHub : IAgentFleetEventHub
{
    public event Func<AgentFleetStatusEvent, Task>? EventPublished;

    public async Task PublishAsync(AgentFleetStatusEvent evt, CancellationToken ct = default)
    {
        var handler = EventPublished;
        if (handler is null)
            return;

        foreach (var subscriber in handler.GetInvocationList().Cast<Func<AgentFleetStatusEvent, Task>>())
            await subscriber(evt).ConfigureAwait(false);
    }
}
