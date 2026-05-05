using System.Collections.Concurrent;
using System.Threading;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed record CancelMetadata(
    string? Actor,
    string? Reason,
    DateTime RequestedAtUtc);

public sealed record AutonomousRunsHealthSnapshot(
    int ActiveRuns,
    int TotalStarted,
    int TotalCompleted,
    int TotalCancelled,
    int TotalFailed,
    int TotalPausedTransitions,
    IReadOnlyList<AutonomousRunHealthItem> Active);

public sealed record AutonomousRunHealthItem(
    Guid RunId,
    DateTime StartedAtUtc,
    bool IsPaused,
    bool IsCancellationRequested,
    int PauseTransitions,
    string CurrentPhase,
    int CurrentIteration,
    int CurrentAttempt,
    CancelMetadata? CancelMetadata);

public sealed record AutonomousRunStateSnapshot(
    Guid RunId,
    DateTime StartedAtUtc,
    bool IsPaused,
    bool IsCancellationRequested,
    int PauseTransitions,
    string CurrentPhase,
    int CurrentIteration,
    int CurrentAttempt,
    CancelMetadata? CancelMetadata);

public interface IAutonomousRunControlService
{
    void RegisterRun(Guid runId, CancellationTokenSource linkedCancellation);
    void CompleteRun(Guid runId, string finalStatus, string? failureReason);

    bool CancelRun(Guid runId, string? actor = null, string? reason = null);
    bool PauseRun(Guid runId);
    bool ResumeRun(Guid runId);

    void UpdateRunProgress(Guid runId, string currentPhase, int currentIteration, int currentAttempt);
    Task WaitIfPausedAsync(Guid runId, CancellationToken ct);
    bool IsCancellationRequested(Guid runId);
    AutonomousRunStateSnapshot? GetRunState(Guid runId);
    AutonomousRunsHealthSnapshot GetHealthSnapshot();
}

public sealed class AutonomousRunControlService : IAutonomousRunControlService
{
    private sealed class RunState
    {
        public required Guid RunId { get; init; }
        public required DateTime StartedAtUtc { get; init; }
        public required CancellationTokenSource Cancellation { get; init; }
        public bool IsPaused;
        public int PauseTransitions;
        public string CurrentPhase = "planning";
        public int CurrentIteration;
        public int CurrentAttempt;
        public CancelMetadata? CancelMetadata;
    }

    private readonly ConcurrentDictionary<Guid, RunState> _active = new();
    private int _totalStarted;
    private int _totalCompleted;
    private int _totalCancelled;
    private int _totalFailed;
    private int _totalPausedTransitions;

    public void RegisterRun(Guid runId, CancellationTokenSource linkedCancellation)
    {
        var state = new RunState
        {
            RunId = runId,
            StartedAtUtc = DateTime.UtcNow,
            Cancellation = linkedCancellation
        };

        _active[runId] = state;
        Interlocked.Increment(ref _totalStarted);
    }

    public void CompleteRun(Guid runId, string finalStatus, string? failureReason)
    {
        if (_active.TryRemove(runId, out _))
        {
            Interlocked.Increment(ref _totalCompleted);
            if (string.Equals(finalStatus, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(failureReason)
                    && failureReason.Contains("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref _totalCancelled);
                }
                else
                {
                    Interlocked.Increment(ref _totalFailed);
                }
            }
        }
    }

    public bool CancelRun(Guid runId, string? actor = null, string? reason = null)
    {
        if (!_active.TryGetValue(runId, out var state))
            return false;

        try
        {
            state.CancelMetadata = new CancelMetadata(actor, reason, DateTime.UtcNow);
            state.Cancellation.Cancel();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool PauseRun(Guid runId)
    {
        if (!_active.TryGetValue(runId, out var state))
            return false;

        if (!state.IsPaused)
        {
            state.IsPaused = true;
            state.PauseTransitions++;
            Interlocked.Increment(ref _totalPausedTransitions);
        }
        return true;
    }

    public bool ResumeRun(Guid runId)
    {
        if (!_active.TryGetValue(runId, out var state))
            return false;

        if (state.IsPaused)
        {
            state.IsPaused = false;
            state.PauseTransitions++;
            Interlocked.Increment(ref _totalPausedTransitions);
        }
        return true;
    }

    public void UpdateRunProgress(Guid runId, string currentPhase, int currentIteration, int currentAttempt)
    {
        if (!_active.TryGetValue(runId, out var state))
            return;

        state.CurrentPhase = string.IsNullOrWhiteSpace(currentPhase) ? state.CurrentPhase : currentPhase;
        state.CurrentIteration = currentIteration;
        state.CurrentAttempt = currentAttempt;
    }

    public async Task WaitIfPausedAsync(Guid runId, CancellationToken ct)
    {
        while (_active.TryGetValue(runId, out var state) && state.IsPaused)
        {
            await Task.Delay(300, ct);
        }
    }

    public bool IsCancellationRequested(Guid runId)
    {
        return _active.TryGetValue(runId, out var state) && state.Cancellation.IsCancellationRequested;
    }

    public AutonomousRunStateSnapshot? GetRunState(Guid runId)
    {
        if (!_active.TryGetValue(runId, out var state))
            return null;

        return new AutonomousRunStateSnapshot(
            RunId: state.RunId,
            StartedAtUtc: state.StartedAtUtc,
            IsPaused: state.IsPaused,
            IsCancellationRequested: state.Cancellation.IsCancellationRequested,
            PauseTransitions: state.PauseTransitions,
            CurrentPhase: state.CurrentPhase,
            CurrentIteration: state.CurrentIteration,
            CurrentAttempt: state.CurrentAttempt,
            CancelMetadata: state.CancelMetadata);
    }

    public AutonomousRunsHealthSnapshot GetHealthSnapshot()
    {
        var active = _active.Values
            .Select(x => new AutonomousRunHealthItem(
                RunId: x.RunId,
                StartedAtUtc: x.StartedAtUtc,
                IsPaused: x.IsPaused,
                IsCancellationRequested: x.Cancellation.IsCancellationRequested,
                PauseTransitions: x.PauseTransitions,
                CurrentPhase: x.CurrentPhase,
                CurrentIteration: x.CurrentIteration,
                CurrentAttempt: x.CurrentAttempt,
                CancelMetadata: x.CancelMetadata))
            .OrderBy(x => x.StartedAtUtc)
            .ToList();

        return new AutonomousRunsHealthSnapshot(
            ActiveRuns: active.Count,
            TotalStarted: Volatile.Read(ref _totalStarted),
            TotalCompleted: Volatile.Read(ref _totalCompleted),
            TotalCancelled: Volatile.Read(ref _totalCancelled),
            TotalFailed: Volatile.Read(ref _totalFailed),
            TotalPausedTransitions: Volatile.Read(ref _totalPausedTransitions),
            Active: active);
    }
}
