namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public interface ISpaceConcurrencyGate
{
    Task<IDisposable> AcquireLlmSlotAsync(Guid spaceId, CancellationToken ct = default);

    int GetActiveCount(Guid spaceId);
}

public sealed class SpaceConcurrencyGate : ISpaceConcurrencyGate
{
    private readonly AgentSpaceOptions _options;
    private readonly object _lock = new();
    private readonly Dictionary<Guid, GateState> _gates = new();

    public SpaceConcurrencyGate(Microsoft.Extensions.Options.IOptions<AgentSpaceOptions> options) =>
        _options = options.Value;

    public async Task<IDisposable> AcquireLlmSlotAsync(Guid spaceId, CancellationToken ct = default)
    {
        var gate = GetOrCreate(spaceId);
        await gate.Semaphore.WaitAsync(ct).ConfigureAwait(false);
        Interlocked.Increment(ref gate.Active);
        return new ReleaseHandle(gate);
    }

    public int GetActiveCount(Guid spaceId)
    {
        lock (_lock)
            return _gates.TryGetValue(spaceId, out var g) ? g.Active : 0;
    }

    private GateState GetOrCreate(Guid spaceId)
    {
        lock (_lock)
        {
            if (!_gates.TryGetValue(spaceId, out var gate))
            {
                gate = new GateState(_options.MaxParallelLlmPerSpace);
                _gates[spaceId] = gate;
            }

            return gate;
        }
    }

    private sealed class GateState(int max)
    {
        public SemaphoreSlim Semaphore { get; } = new(max, max);
        public int Active;
    }

    private sealed class ReleaseHandle(GateState gate) : IDisposable
    {
        public void Dispose()
        {
            Interlocked.Decrement(ref gate.Active);
            gate.Semaphore.Release();
        }
    }
}
