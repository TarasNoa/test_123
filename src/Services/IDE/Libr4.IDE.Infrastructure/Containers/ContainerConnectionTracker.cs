using System.Collections.Concurrent;

namespace Libr4.IDE.Infrastructure.Containers;

/// <summary>
/// Tracks WebSocket connections per workspace for immediate container cleanup
/// When user closes tab, container is destroyed immediately
/// </summary>
public class ContainerConnectionTracker
{
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private readonly IContainerManager _containerManager;
    private readonly ILogger<ContainerConnectionTracker> _logger;

    public ContainerConnectionTracker(
        IContainerManager containerManager,
        ILogger<ContainerConnectionTracker> logger)
    {
        _containerManager = containerManager;
        _logger = logger;
    }

    /// <summary>
    /// Register a new connection for a workspace
    /// </summary>
    public void RegisterConnection(string workspaceId, string connectionId, string userId)
    {
        var info = _connections.GetOrAdd(workspaceId, _ => new ConnectionInfo(workspaceId));
        info.Connections.TryAdd(connectionId, new ConnectionDetails
        {
            ConnectionId = connectionId,
            UserId = userId,
            ConnectedAt = DateTime.UtcNow
        });

        _logger.LogInformation(
            "Connection {ConnectionId} registered for workspace {WorkspaceId}. Total connections: {Count}",
            connectionId, workspaceId, info.Connections.Count);
    }

    /// <summary>
    /// Unregister a connection and cleanup container if no connections left
    /// </summary>
    public async Task UnregisterConnectionAsync(string workspaceId, string connectionId)
    {
        if (_connections.TryGetValue(workspaceId, out var info))
        {
            info.Connections.TryRemove(connectionId, out _);

            _logger.LogInformation(
                "Connection {ConnectionId} unregistered for workspace {WorkspaceId}. Remaining connections: {Count}",
                connectionId, workspaceId, info.Connections.Count);

            // If no connections left, destroy container immediately
            if (info.Connections.IsEmpty)
            {
                _logger.LogInformation(
                    "No connections left for workspace {WorkspaceId}. Initiating immediate container cleanup...",
                    workspaceId);

                await CleanupContainerAsync(workspaceId);
            }
        }
    }

    /// <summary>
    /// Get active connection count for workspace
    /// </summary>
    public int GetConnectionCount(string workspaceId)
    {
        return _connections.TryGetValue(workspaceId, out var info)
            ? info.Connections.Count
            : 0;
    }

    /// <summary>
    /// Force disconnect all connections for workspace
    /// </summary>
    public async Task ForceDisconnectAllAsync(string workspaceId)
    {
        if (_connections.TryRemove(workspaceId, out var info))
        {
            _logger.LogInformation(
                "Force disconnecting {Count} connections for workspace {WorkspaceId}",
                info.Connections.Count, workspaceId);

            await CleanupContainerAsync(workspaceId);
        }
    }

    /// <summary>
    /// Get workspace IDs with zero connections (orphaned)
    /// </summary>
    public IEnumerable<string> GetOrphanedWorkspaces(TimeSpan idleThreshold)
    {
        var cutoff = DateTime.UtcNow - idleThreshold;

        return _connections
            .Where(kvp => kvp.Value.Connections.IsEmpty && kvp.Value.LastDisconnectedAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    private async Task CleanupContainerAsync(string workspaceId)
    {
        try
        {
            // Stop and remove container immediately
            await _containerManager.StopAndRemoveContainerAsync(workspaceId);

            _logger.LogInformation(
                "Container for workspace {WorkspaceId} cleaned up immediately due to no active connections",
                workspaceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to cleanup container for workspace {WorkspaceId}",
                workspaceId);
        }
    }

    /// <summary>
    /// Connection info for a workspace
    /// </summary>
    private class ConnectionInfo
    {
        public string WorkspaceId { get; }
        public ConcurrentDictionary<string, ConnectionDetails> Connections { get; }
        public DateTime LastDisconnectedAt { get; set; }

        public ConnectionInfo(string workspaceId)
        {
            WorkspaceId = workspaceId;
            Connections = new ConcurrentDictionary<string, ConnectionDetails>();
        }
    }

    /// <summary>
    /// Details for a single connection
    /// </summary>
    private class ConnectionDetails
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
    }
}
