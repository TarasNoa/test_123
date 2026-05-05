using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.ShadowWorkspace;

/// <summary>
/// Tracks container connections for immediate cleanup
/// </summary>
public class ContainerConnectionTracker
{
    private readonly ILogger<ContainerConnectionTracker> _logger;
    private readonly Dictionary<string, List<ConnectionEntry>> _connections = new();
    private readonly object _lock = new();

    public ContainerConnectionTracker(ILogger<ContainerConnectionTracker> logger)
    {
        _logger = logger;
    }

    public void TrackConnection(string containerId, string connectionId, string connectionType)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(containerId, out var list))
            {
                list = new List<ConnectionEntry>();
                _connections[containerId] = list;
            }
            
            list.Add(new ConnectionEntry
            {
                ConnectionId = connectionId,
                ConnectionType = connectionType,
                ConnectedAt = DateTime.UtcNow
            });
        }
        
        _logger.LogDebug("Tracked {ConnectionType} connection {ConnectionId} for container {ContainerId}", 
            connectionType, connectionId, containerId);
    }

    public void RemoveConnection(string containerId, string connectionId)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(containerId, out var list))
            {
                list.RemoveAll(c => c.ConnectionId == connectionId);
                
                if (list.Count == 0)
                {
                    _connections.Remove(containerId);
                }
            }
        }
        
        _logger.LogDebug("Removed connection {ConnectionId} from container {ContainerId}", 
            connectionId, containerId);
    }

    public IReadOnlyList<string> GetConnections(string containerId)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(containerId, out var list))
            {
                return list.Select(c => c.ConnectionId).ToList();
            }
            return Array.Empty<string>();
        }
    }

    public void ClearConnections(string containerId)
    {
        lock (_lock)
        {
            _connections.Remove(containerId);
        }
        
        _logger.LogInformation("Cleared all connections for container {ContainerId}", containerId);
    }

    private class ConnectionEntry
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string ConnectionType { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
    }
}
