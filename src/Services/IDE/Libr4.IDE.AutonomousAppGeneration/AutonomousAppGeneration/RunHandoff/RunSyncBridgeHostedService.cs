using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

/// <summary>
/// Watches registered run sync workspaces and publishes <see cref="WorkspaceSyncDelta"/> via <see cref="RunSyncHub"/>.
/// </summary>
public sealed class RunSyncBridgeHostedService : IHostedService
{
    private readonly IRunSyncCoordinator _coordinator;
    private readonly RunSyncHub _hub;
    private readonly RunSyncOptions _options;
    private readonly ILogger<RunSyncBridgeHostedService> _logger;
    private readonly Dictionary<Guid, FileSystemWatcher> _watchers = new();
    private readonly object _sync = new();

    public RunSyncBridgeHostedService(
        IRunSyncCoordinator coordinator,
        RunSyncHub hub,
        IOptions<RunSyncOptions> options,
        ILogger<RunSyncBridgeHostedService> logger)
    {
        _coordinator = coordinator;
        _hub = hub;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            _watchers.Clear();
        }

        return Task.CompletedTask;
    }

    public void StartWatching(Guid runId)
    {
        if (!_options.Enabled || !_coordinator.TryGetSession(runId, out var session))
            return;

        lock (_sync)
        {
            if (_watchers.ContainsKey(runId))
                return;

            if (!Directory.Exists(session.WorkspaceRoot))
            {
                _logger.LogWarning("Run sync watch skipped; missing workspace run={RunId}", runId);
                return;
            }

            var watcher = new FileSystemWatcher(session.WorkspaceRoot)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            void Raise(FileSystemEventArgs e, WorkspaceSyncDeltaKind kind)
            {
                if (ShouldIgnore(e.FullPath, session.WorkspaceRoot))
                    return;

                var rel = Path.GetRelativePath(session.WorkspaceRoot, e.FullPath).Replace('\\', '/');
                var change = new WorkspaceFileChange(
                    runId,
                    rel,
                    kind switch
                    {
                        WorkspaceSyncDeltaKind.Deleted => WorkspaceFileChangeKind.Deleted,
                        WorkspaceSyncDeltaKind.Created => WorkspaceFileChangeKind.Created,
                        _ => WorkspaceFileChangeKind.Modified
                    },
                    DateTime.UtcNow);

                var delta = _coordinator.CreateDeltaFromFileChange(runId, session.Role, change);
                if (delta is null)
                    return;

                _ = _hub.BroadcastDeltaAsync(delta, originConnectionId: null);
            }

            watcher.Created += (_, e) => Raise(e, WorkspaceSyncDeltaKind.Created);
            watcher.Changed += (_, e) => Raise(e, WorkspaceSyncDeltaKind.Modified);
            watcher.Deleted += (_, e) => Raise(e, WorkspaceSyncDeltaKind.Deleted);
            watcher.Renamed += (_, e) => Raise(e, WorkspaceSyncDeltaKind.Modified);

            _watchers[runId] = watcher;
            _logger.LogInformation("Run sync watcher started run={RunId} root={Root}", runId, session.WorkspaceRoot);
        }
    }

    public void StopWatching(Guid runId)
    {
        lock (_sync)
        {
            if (_watchers.TryGetValue(runId, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                _watchers.Remove(runId);
            }
        }
    }

    private static bool ShouldIgnore(string fullPath, string workspaceRoot)
    {
        var rel = Path.GetRelativePath(workspaceRoot, fullPath).Replace('\\', '/');
        return rel.StartsWith(".libr4/", StringComparison.OrdinalIgnoreCase)
               || rel.Contains("/handoff/sync-conflicts/", StringComparison.OrdinalIgnoreCase)
               || rel.Equals(".libr4", StringComparison.OrdinalIgnoreCase);
    }
}
