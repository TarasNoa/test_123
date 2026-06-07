namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

public interface IAgentBackend
{
    AgentBackendKind Kind { get; }

    Task<AgentBackendHandle> SpawnAsync(AgentBackendSpawnRequest request, CancellationToken ct = default);

    Task SendMessageAsync(string backendInstanceId, string message, CancellationToken ct = default);

    IAsyncEnumerable<AgentBackendEvent> StreamEventsAsync(string backendInstanceId, CancellationToken ct = default);

    Task CancelAsync(string backendInstanceId, CancellationToken ct = default);

    Task<AgentBackendStatus> GetStatusAsync(string backendInstanceId, CancellationToken ct = default);
}

public interface IAgentBackendRegistry
{
    IAgentBackend Resolve(AgentBackendDescriptor descriptor);

    IReadOnlyList<AgentBackendKind> SupportedKinds { get; }
}
