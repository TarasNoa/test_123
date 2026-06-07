using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;

namespace Libr4.IDE.Api;

public sealed class AgentRuntimeWebSocketBridge : IHostedService
{
    private readonly IAgentRuntimeEventHub _hub;
    private readonly IAgentEventEmitter _emitter;
    private readonly AgentEventWebSocketHandler _webSocket;

    public AgentRuntimeWebSocketBridge(
        IAgentRuntimeEventHub hub,
        IAgentEventEmitter emitter,
        AgentEventWebSocketHandler webSocket)
    {
        _hub = hub;
        _emitter = emitter;
        _webSocket = webSocket;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _hub.EventPublished += OnRuntimeEventAsync;
        _emitter.EventPublished += OnAgentEventAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hub.EventPublished -= OnRuntimeEventAsync;
        _emitter.EventPublished -= OnAgentEventAsync;
        return Task.CompletedTask;
    }

    private Task OnRuntimeEventAsync(AgentRuntimePublishedEvent evt) =>
        _webSocket.BroadcastRuntimeNdjsonAsync(evt);

    private Task OnAgentEventAsync(AgentEvent evt) =>
        _webSocket.BroadcastEventAsync(evt);
}
