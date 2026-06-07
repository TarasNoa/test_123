using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

/// <summary>
/// Invalidates the codebase index cache when workspace files change via
/// <see cref="IWorkspaceSyncService"/> (bind-mount shadow workspaces).
/// </summary>
public sealed class FastContextWorkspaceSyncBridge : IHostedService
{
    private readonly IWorkspaceSyncService _sync;
    private readonly IWorkspacePool _pool;
    private readonly ICodebaseIndex _index;
    private readonly ILogger<FastContextWorkspaceSyncBridge> _logger;

    public FastContextWorkspaceSyncBridge(
        IWorkspaceSyncService sync,
        IWorkspacePool pool,
        ICodebaseIndex index,
        ILogger<FastContextWorkspaceSyncBridge> logger)
    {
        _sync = sync;
        _pool = pool;
        _index = index;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sync.OnFileChanged += HandleFileChanged;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sync.OnFileChanged -= HandleFileChanged;
        return Task.CompletedTask;
    }

    private void HandleFileChanged(WorkspaceFileChange change)
    {
        var handle = _pool.ListActive().FirstOrDefault(h => h.WorkspaceId == change.WorkspaceId);
        if (handle is null)
            return;

        _logger.LogDebug(
            "Fast context index invalidate workspace={WorkspaceId} path={Path} kind={Kind}",
            change.WorkspaceId,
            change.RelativePath,
            change.Kind);

        _ = _index.InvalidateAsync(handle.HostPath);
    }
}
