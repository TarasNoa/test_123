namespace Libr4.Gateway.Infrastructure.Rust;

/// <summary>Production circuit breaker state backed by Rust gateway core with in-memory fallback.</summary>
public sealed class RustCircuitBreakerState : ICircuitBreakerState
{
    public bool IsOpen(string key) =>
        RustGatewayCoreBridge.IsAvailable
            ? RustGatewayCoreBridge.IsCircuitOpen(key)
            : Fallback.IsOpen(key);

    public void RecordSuccess(string key)
    {
        if (RustGatewayCoreBridge.IsAvailable)
            RustGatewayCoreBridge.RecordCircuitSuccess(key);
        else
            Fallback.RecordSuccess(key);
    }

    public void RecordFailure(string key)
    {
        if (RustGatewayCoreBridge.IsAvailable)
            RustGatewayCoreBridge.RecordCircuitFailure(key);
        else
            Fallback.RecordFailure(key);
    }

    public CircuitStats GetStats(string key) => Fallback.GetStats(key);

    private static readonly InMemoryCircuitBreakerFallback Fallback = new();
}

public interface ICircuitBreakerState
{
    bool IsOpen(string key);
    void RecordSuccess(string key);
    void RecordFailure(string key);
    CircuitStats GetStats(string key);
}

public sealed class CircuitStats
{
    public string Status { get; init; } = "Closed";
    public int ConsecutiveFailures { get; init; }
    public int ConsecutiveSuccesses { get; init; }
    public int RecentFailureCount { get; init; }
}

internal sealed class InMemoryCircuitBreakerFallback : ICircuitBreakerState
{
    private readonly Dictionary<string, (int Failures, bool Open)> _state = new(StringComparer.OrdinalIgnoreCase);

    public bool IsOpen(string key) => _state.TryGetValue(key, out var s) && s.Open;

    public void RecordSuccess(string key)
    {
        if (_state.ContainsKey(key))
            _state[key] = (0, false);
    }

    public void RecordFailure(string key)
    {
        var failures = _state.TryGetValue(key, out var s) ? s.Failures + 1 : 1;
        _state[key] = (failures, failures >= 5);
    }

    public CircuitStats GetStats(string key)
    {
        if (!_state.TryGetValue(key, out var s))
            return new CircuitStats();

        return new CircuitStats
        {
            Status = s.Open ? "Open" : "Closed",
            ConsecutiveFailures = s.Failures,
            RecentFailureCount = s.Failures
        };
    }
}
