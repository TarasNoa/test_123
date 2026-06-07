using System.Collections.Concurrent;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;

public sealed class RoleModelCircuitBreaker
{
    private readonly ConcurrentDictionary<string, CircuitState> _states = new(StringComparer.OrdinalIgnoreCase);
    private readonly AgentModelRoutingOptions _options;

    public RoleModelCircuitBreaker(IOptions<AgentModelRoutingOptions> options) => _options = options.Value;

    public bool IsOpen(string role, string model) => IsOpen(BuildKey(role, model));

    public void OnSuccess(string role, string model) => OnSuccess(BuildKey(role, model));

    public void OnFailure(string role, string model) => OnFailure(BuildKey(role, model));

    private bool IsOpen(string key)
    {
        var state = _states.GetOrAdd(key, _ => CircuitState.Closed());
        if (FSharpAlgorithmsBridge.ShouldRoleCircuitHalfOpen(state, _options.RoleCircuitOpenSeconds))
            _states[key] = state = state.ToHalfOpen();

        return FSharpAlgorithmsBridge.IsRoleCircuitOpen(state, _options.RoleCircuitOpenSeconds);
    }

    private void OnSuccess(string key)
    {
        var state = _states.GetOrAdd(key, _ => CircuitState.Closed());
        _states[key] = FSharpAlgorithmsBridge.AdvanceRoleCircuitOnSuccess(state);
    }

    private void OnFailure(string key)
    {
        var state = _states.GetOrAdd(key, _ => CircuitState.Closed());
        _states[key] = FSharpAlgorithmsBridge.AdvanceRoleCircuitOnFailure(
            state,
            _options.RoleCircuitFailureThreshold);
    }

    private static string BuildKey(string role, string model) =>
        FSharpAlgorithmsBridge.BuildRoleModelCircuitKey(role, model);

    internal readonly struct CircuitState
    {
        public int Current { get; init; }
        public int Failures { get; init; }
        public DateTime? OpenedAtUtc { get; init; }

        public static CircuitState Closed() => new() { Current = 0, Failures = 0, OpenedAtUtc = null };

        public CircuitState ToHalfOpen() => this with { Current = 2 };
    }
}
