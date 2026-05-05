namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

public sealed record RuntimeAttemptRecord(
    DateTime TimestampUtc,
    string PreferredProvider,
    string AttemptedProvider,
    bool Succeeded,
    bool UsedAsFallback,
    string? SessionId,
    string? ErrorMessage);

public sealed record RuntimeDiagnosticsSnapshot(
    string PreferredProvider,
    int TotalAttempts,
    int FailedAttempts,
    string? LastSuccessfulProvider,
    DateTime? LastSuccessfulAtUtc,
    IReadOnlyList<RuntimeAttemptRecord> RecentAttempts);

public interface IRuntimeDiagnostics
{
    void RecordAttempt(
        string preferredProvider,
        string attemptedProvider,
        bool succeeded,
        bool usedAsFallback,
        string? sessionId,
        string? errorMessage);

    RuntimeDiagnosticsSnapshot GetSnapshot();
}

public sealed class InMemoryRuntimeDiagnostics : IRuntimeDiagnostics
{
    private const int MaxRecords = 200;
    private readonly object _gate = new();
    private readonly Queue<RuntimeAttemptRecord> _records = new();
    private string _preferredProvider = "docker";

    public void RecordAttempt(
        string preferredProvider,
        string attemptedProvider,
        bool succeeded,
        bool usedAsFallback,
        string? sessionId,
        string? errorMessage)
    {
        lock (_gate)
        {
            _preferredProvider = preferredProvider;
            _records.Enqueue(new RuntimeAttemptRecord(
                TimestampUtc: DateTime.UtcNow,
                PreferredProvider: preferredProvider,
                AttemptedProvider: attemptedProvider,
                Succeeded: succeeded,
                UsedAsFallback: usedAsFallback,
                SessionId: sessionId,
                ErrorMessage: errorMessage));

            while (_records.Count > MaxRecords)
                _records.Dequeue();
        }
    }

    public RuntimeDiagnosticsSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            var records = _records.ToList();
            var failed = records.Count(x => !x.Succeeded);
            var lastOk = records.LastOrDefault(x => x.Succeeded);

            return new RuntimeDiagnosticsSnapshot(
                PreferredProvider: _preferredProvider,
                TotalAttempts: records.Count,
                FailedAttempts: failed,
                LastSuccessfulProvider: lastOk?.AttemptedProvider,
                LastSuccessfulAtUtc: lastOk?.TimestampUtc,
                RecentAttempts: records.TakeLast(25).ToList());
        }
    }
}
