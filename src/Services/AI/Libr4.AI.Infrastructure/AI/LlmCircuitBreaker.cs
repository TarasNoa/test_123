using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.AI;

/// <summary>
/// P1-5 (audit roadmap): Lightweight per-provider circuit breaker, implemented without Polly
/// to avoid external package dependency.
///
/// State machine:  Closed ──(failures >= threshold)──▶ Open
///                 Open   ──(half-open timeout)──────▶ HalfOpen
///                 HalfOpen ──(success)──────────────▶ Closed
///                 HalfOpen ──(failure)──────────────▶ Open
/// </summary>
public sealed class LlmCircuitBreaker
{
    private enum State { Closed, Open, HalfOpen }

    private readonly record struct ProviderState(
        State Current,
        int ConsecutiveFailures,
        DateTime? OpenedAtUtc);

    private readonly ConcurrentDictionary<string, ProviderState> _states = new(StringComparer.OrdinalIgnoreCase);
    private readonly LlmCircuitBreakerOptions _options;
    private readonly ILogger<LlmCircuitBreaker> _logger;

    public LlmCircuitBreaker(LlmCircuitBreakerOptions? options = null, ILogger<LlmCircuitBreaker>? logger = null)
    {
        _options = options ?? new LlmCircuitBreakerOptions();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<LlmCircuitBreaker>.Instance;
    }

    /// <summary>
    /// Returns true if the provider circuit is currently open (requests should be rejected).
    /// </summary>
    public bool IsOpen(string providerId)
    {
        var state = GetOrCreate(providerId);
        if (state.Current == State.Open)
        {
            // Attempt transition to HalfOpen after timeout.
            if (state.OpenedAtUtc.HasValue &&
                DateTime.UtcNow - state.OpenedAtUtc.Value >= _options.OpenDuration)
            {
                Transition(providerId, state with { Current = State.HalfOpen });
                return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>Records a successful call and resets failure counter if in HalfOpen.</summary>
    public void OnSuccess(string providerId)
    {
        var state = GetOrCreate(providerId);
        if (state.Current == State.HalfOpen || state.ConsecutiveFailures > 0)
        {
            _logger.LogDebug("[CircuitBreaker] Provider {ProviderId} recovered. Transitioning to Closed.", providerId);
            Transition(providerId, new ProviderState(State.Closed, 0, null));
        }
    }

    /// <summary>Records a failed call and opens the circuit if the threshold is reached.</summary>
    public void OnFailure(string providerId)
    {
        var state = GetOrCreate(providerId);
        if (state.Current == State.Open)
            return;

        var newFailures = state.ConsecutiveFailures + 1;
        if (newFailures >= _options.FailureThreshold)
        {
            _logger.LogWarning(
                "[CircuitBreaker] Provider {ProviderId} exceeded failure threshold ({Threshold}). Opening circuit for {Duration}.",
                providerId, _options.FailureThreshold, _options.OpenDuration);
            Transition(providerId, new ProviderState(State.Open, newFailures, DateTime.UtcNow));
        }
        else
        {
            Transition(providerId, state with { ConsecutiveFailures = newFailures });
        }
    }

    /// <summary>Returns a snapshot of circuit states for diagnostics / telemetry.</summary>
    public IReadOnlyDictionary<string, string> GetStateSnapshot() =>
        _states.ToDictionary(kv => kv.Key, kv => kv.Value.Current.ToString(), StringComparer.OrdinalIgnoreCase);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private ProviderState GetOrCreate(string providerId) =>
        _states.GetOrAdd(providerId, _ => new ProviderState(State.Closed, 0, null));

    private void Transition(string providerId, ProviderState next) =>
        _states[providerId] = next;
}

/// <summary>Configuration knobs for <see cref="LlmCircuitBreaker"/>.</summary>
public sealed class LlmCircuitBreakerOptions
{
    /// <summary>How many consecutive failures before the circuit opens. Default: 5.</summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>How long to keep the circuit open before attempting HalfOpen. Default: 30s.</summary>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);
}
