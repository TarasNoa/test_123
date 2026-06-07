namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

using Microsoft.Extensions.Logging;

public sealed class RunSyncHub
{
    private readonly IRunSyncCoordinator _coordinator;
    private readonly ILogger<RunSyncHub> _logger;
    private readonly object _sync = new();
    private readonly Dictionary<Guid, HashSet<string>> _connectionsByRun = new();
    private readonly Dictionary<string, Func<WorkspaceSyncDelta, Task>> _senders = new(StringComparer.Ordinal);

    public RunSyncHub(IRunSyncCoordinator coordinator, ILogger<RunSyncHub> logger)
    {
        _coordinator = coordinator;
        _logger = logger;
    }

    public IReadOnlyCollection<Guid> GetActiveRunIds()
    {
        lock (_sync)
            return _connectionsByRun.Keys.ToList();
    }

    public void RegisterConnection(Guid runId, string connectionId, Func<WorkspaceSyncDelta, Task> sender)
    {
        lock (_sync)
        {
            if (!_connectionsByRun.TryGetValue(runId, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                _connectionsByRun[runId] = set;
            }

            set.Add(connectionId);
            _senders[connectionId] = sender;
        }
    }

    public void UnregisterConnection(Guid runId, string connectionId)
    {
        lock (_sync)
        {
            _senders.Remove(connectionId);
            if (_connectionsByRun.TryGetValue(runId, out var set))
            {
                set.Remove(connectionId);
                if (set.Count == 0)
                    _connectionsByRun.Remove(runId);
            }
        }
    }

    public async Task<RunSyncApplyResult> IngestDeltaAsync(
        WorkspaceSyncDelta delta,
        string? originConnectionId)
    {
        var result = await _coordinator.ApplyDeltaAsync(delta).ConfigureAwait(false);
        if (result.Status is RunSyncApplyStatus.Applied or RunSyncApplyStatus.ConflictRecorded)
            await BroadcastDeltaAsync(delta, originConnectionId).ConfigureAwait(false);

        return result;
    }

    public async Task BroadcastDeltaAsync(WorkspaceSyncDelta delta, string? originConnectionId)
    {
        List<Func<WorkspaceSyncDelta, Task>> targets;
        lock (_sync)
        {
            if (!_connectionsByRun.TryGetValue(delta.RunId, out var set))
                return;

            targets = set
                .Where(id => !string.Equals(id, originConnectionId, StringComparison.Ordinal))
                .Select(id => _senders[id])
                .ToList();
        }

        foreach (var send in targets)
        {
            try
            {
                await send(delta).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to push run sync delta to connection");
            }
        }
    }
}
