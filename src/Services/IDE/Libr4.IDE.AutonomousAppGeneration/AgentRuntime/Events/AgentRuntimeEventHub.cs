namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;

public interface IAgentRuntimeEventHub
{
    event Func<AgentRuntimePublishedEvent, Task>? EventPublished;
    Task PublishAsync(AgentRuntimePublishedEvent evt, CancellationToken ct = default);
}

public sealed record AgentRuntimePublishedEvent(Guid RunId, string EventType, string PayloadJson, DateTimeOffset TimestampUtc);

public sealed class AgentRuntimeEventHub : IAgentRuntimeEventHub
{
    public event Func<AgentRuntimePublishedEvent, Task>? EventPublished;

    public async Task PublishAsync(AgentRuntimePublishedEvent evt, CancellationToken ct = default)
    {
        var handler = EventPublished;
        if (handler is null)
            return;

        foreach (var subscriber in handler.GetInvocationList().Cast<Func<AgentRuntimePublishedEvent, Task>>())
            await subscriber(evt).ConfigureAwait(false);
    }
}
