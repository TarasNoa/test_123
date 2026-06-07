namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

public sealed class AgentBackendRegistry : IAgentBackendRegistry
{
    private readonly IReadOnlyDictionary<AgentBackendKind, IAgentBackend> _backends;

    public AgentBackendRegistry(IEnumerable<IAgentBackend> backends)
    {
        _backends = backends.ToDictionary(b => b.Kind);
    }

    public IReadOnlyList<AgentBackendKind> SupportedKinds =>
        _backends.Keys.OrderBy(k => k.ToString()).ToList();

    public IAgentBackend Resolve(AgentBackendDescriptor descriptor)
    {
        if (_backends.TryGetValue(descriptor.Kind, out var backend))
            return backend;

        throw new NotSupportedException($"agent_backend_not_registered:{descriptor.Kind}");
    }
}
