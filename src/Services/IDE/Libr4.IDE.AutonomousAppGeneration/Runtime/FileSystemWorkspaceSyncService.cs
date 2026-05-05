using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// Bidirectional sync implementation built on top of bind-mount semantics:
/// the host directory is physically the same storage as the one seen from
/// the guest, so the only job left is to watch the host side and emit
/// change events to the IDE.
/// </summary>
public sealed class FileSystemWorkspaceSyncService : IWorkspaceSyncService
{
    private readonly ILogger<FileSystemWorkspaceSyncService> _logger;
    private readonly ConcurrentDictionary<Guid, WatcherBundle> _watchers = new();
    private bool _disposed;

    public event Action<WorkspaceFileChange>? OnFileChanged;

    public FileSystemWorkspaceSyncService(ILogger<FileSystemWorkspaceSyncService> logger)
    {
        _logger = logger;
    }

    public void StartWatching(WorkspaceHandle handle)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FileSystemWorkspaceSyncService));
        if (_watchers.ContainsKey(handle.WorkspaceId)) return;
        if (!Directory.Exists(handle.HostPath))
        {
            _logger.LogWarning("Cannot watch missing directory {Path}", handle.HostPath);
            return;
        }

        var watcher = new FileSystemWatcher(handle.HostPath)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.CreationTime
        };

        void Raise(WorkspaceFileChangeKind kind, string fullPath)
        {
            var rel = Path.GetRelativePath(handle.HostPath, fullPath).Replace('\\', '/');
            try
            {
                OnFileChanged?.Invoke(new WorkspaceFileChange(
                    handle.WorkspaceId, rel, kind, DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sync subscriber threw");
            }
        }

        watcher.Created += (_, e) => Raise(WorkspaceFileChangeKind.Created, e.FullPath);
        watcher.Changed += (_, e) => Raise(WorkspaceFileChangeKind.Modified, e.FullPath);
        watcher.Deleted += (_, e) => Raise(WorkspaceFileChangeKind.Deleted, e.FullPath);
        watcher.Renamed += (_, e) => Raise(WorkspaceFileChangeKind.Renamed, e.FullPath);

        _watchers[handle.WorkspaceId] = new WatcherBundle(watcher);
        _logger.LogInformation(
            "Sync watcher started for workspace {Id} at {Path}",
            handle.WorkspaceId, handle.HostPath);
    }

    public void StopWatching(Guid workspaceId)
    {
        if (_watchers.TryRemove(workspaceId, out var bundle))
        {
            bundle.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        foreach (var bundle in _watchers.Values) bundle.Dispose();
        _watchers.Clear();
        return ValueTask.CompletedTask;
    }

    private sealed class WatcherBundle : IDisposable
    {
        private readonly FileSystemWatcher _w;
        public WatcherBundle(FileSystemWatcher w) { _w = w; }
        public void Dispose()
        {
            try { _w.EnableRaisingEvents = false; _w.Dispose(); }
            catch (Exception ex)
            {
                // Best-effort cleanup - ignore watcher disposal errors
                System.Diagnostics.Debug.WriteLine($"Failed to dispose FileSystemWatcher: {ex.Message}");
            }
        }
    }
}
