using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Libr4.Collaboration.Infrastructure.Connections;

public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new(); // userId -> connectionIds
    private readonly ConcurrentDictionary<string, HashSet<string>> _roomConnections = new(); // roomId -> connectionIds
    private readonly ConcurrentDictionary<string, HashSet<string>> _connectionRooms = new(); // connectionId -> roomIds
    private readonly ILogger<ConnectionManager> _logger;

    private readonly ConnectionStatistics _statistics = new()
    {
        StartTime = DateTime.UtcNow
    };

    public ConnectionManager(ILogger<ConnectionManager> logger)
    {
        _logger = logger;
    }

    public Task ConnectAsync(string connectionId, string userId)
    {
        var connectionInfo = new ConnectionInfo
        {
            ConnectionId = connectionId,
            UserId = userId,
            ConnectedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };

        _connections.TryAdd(connectionId, connectionInfo);

        // Associate with user
        if (!_userConnections.ContainsKey(userId))
        {
            _userConnections[userId] = new HashSet<string>();
        }
        lock (_userConnections[userId])
        {
            _userConnections[userId].Add(connectionId);
        }

        // Update statistics
        Interlocked.Increment(ref _statistics.TotalConnections);
        Interlocked.Increment(ref _statistics.ActiveConnections);

        _logger.LogInformation("WebSocket connected: {ConnectionId} for user {UserId}", connectionId, userId);

        return Task.CompletedTask;
    }

    public Task DisconnectAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connectionInfo))
        {
            var userId = connectionInfo.UserId;

            // Remove from user connections
            if (!string.IsNullOrEmpty(userId) && _userConnections.TryGetValue(userId, out var userConns))
            {
                lock (userConns)
                {
                    userConns.Remove(connectionId);
                }
                if (userConns.Count == 0)
                {
                    HashSet<string>? removed;
                    _userConnections.TryRemove(userId, out removed);
                }
            }

            // Remove from rooms
            if (_connectionRooms.TryGetValue(connectionId, out var roomIds))
            {
                foreach (var roomId in roomIds)
                {
                    if (_roomConnections.TryGetValue(roomId, out var roomConns))
                    {
                        lock (roomConns)
                        {
                            roomConns.Remove(connectionId);
                        }
                        if (roomConns.Count == 0)
                        {
                            HashSet<string>? removed;
                            _roomConnections.TryRemove(roomId, out removed);
                        }
                    }
                }
                HashSet<string>? removedRoom;
                _connectionRooms.TryRemove(connectionId, out removedRoom);
            }

            // Update statistics
            Interlocked.Decrement(ref _statistics.ActiveConnections);

            _logger.LogInformation("WebSocket disconnected: {ConnectionId}", connectionId);
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetUserIdAsync(string connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var connectionInfo))
        {
            return Task.FromResult<string?>(connectionInfo.UserId);
        }

        return Task.FromResult<string?>(null);
    }

    public Task JoinRoomAsync(string connectionId, string roomId)
    {
        if (!_connections.ContainsKey(connectionId))
        {
            return Task.CompletedTask;
        }

        // Add connection to room
        if (!_roomConnections.ContainsKey(roomId))
        {
            _roomConnections[roomId] = new HashSet<string>();
        }
        lock (_roomConnections[roomId])
        {
            _roomConnections[roomId].Add(connectionId);
        }

        // Add room to connection
        if (!_connectionRooms.ContainsKey(connectionId))
        {
            _connectionRooms[connectionId] = new HashSet<string>();
        }
        lock (_connectionRooms[connectionId])
        {
            _connectionRooms[connectionId].Add(roomId);
        }

        _logger.LogInformation("Connection {ConnectionId} joined room {RoomId}", connectionId, roomId);

        return Task.CompletedTask;
    }

    public Task LeaveRoomAsync(string connectionId, string roomId)
    {
        // Remove connection from room
        if (_roomConnections.TryGetValue(roomId, out var roomConns))
        {
            lock (roomConns)
            {
                roomConns.Remove(connectionId);
            }
            if (roomConns.Count == 0)
            {
                HashSet<string>? removed;
                _roomConnections.TryRemove(roomId, out removed);
            }
        }

        // Remove room from connection
        if (_connectionRooms.TryGetValue(connectionId, out var roomIds))
        {
            lock (roomIds)
            {
                roomIds.Remove(roomId);
            }
            if (roomIds.Count == 0)
            {
                HashSet<string>? removed;
                _connectionRooms.TryRemove(connectionId, out removed);
            }
        }

        _logger.LogInformation("Connection {ConnectionId} left room {RoomId}", connectionId, roomId);

        return Task.CompletedTask;
    }

    public Task SendToRoomAsync(string roomId, object message, string? excludeConnectionId = null)
    {
        // This will be handled by SignalR Groups
        // This method is for external services to send messages to rooms
        return Task.CompletedTask;
    }

    public Task SendToUserAsync(string userId, object message)
    {
        // This will be handled by SignalR
        // This method is for external services to send messages to users
        return Task.CompletedTask;
    }

    public Task SendToConnectionAsync(string connectionId, object message)
    {
        // This will be handled by SignalR
        // This method is for external services to send messages to specific connections
        return Task.CompletedTask;
    }

    public Task BroadcastAsync(object message, string? excludeUserId = null)
    {
        // This will be handled by SignalR
        // This method is for external services to broadcast messages
        return Task.CompletedTask;
    }

    public Task<ConnectionStatistics> GetStatisticsAsync()
    {
        var uptime = DateTime.UtcNow - _statistics.StartTime;
        var avgMessagesPerSecond = uptime.TotalSeconds > 0
            ? _statistics.TotalMessagesSent / uptime.TotalSeconds
            : 0;

        _statistics.UptimeSeconds = uptime.TotalSeconds;
        _statistics.UniqueUsers = _userConnections.Count;
        _statistics.TotalRooms = _roomConnections.Count;
        _statistics.AverageMessagesPerSecond = avgMessagesPerSecond;

        return Task.FromResult(_statistics);
    }

    public Task CleanupInactiveConnectionsAsync(int maxInactiveMinutes = 30)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-maxInactiveMinutes);
        var inactiveConnections = _connections
            .Where(kvp => kvp.Value.LastActivity < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var connectionId in inactiveConnections)
        {
            _logger.LogInformation("Cleaning up inactive connection: {ConnectionId}", connectionId);
            ConnectionInfo? removed;
            _connections.TryRemove(connectionId, out removed);
        }

        return Task.CompletedTask;
    }

    public void IncrementMessagesSent()
    {
        Interlocked.Increment(ref _statistics.TotalMessagesSent);
    }

    public void IncrementMessagesReceived()
    {
        Interlocked.Increment(ref _statistics.TotalMessagesReceived);
    }

    public void UpdateLastActivity(string connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var connectionInfo))
        {
            connectionInfo.LastActivity = DateTime.UtcNow;
        }
    }
}

public class ConnectionInfo
{
    public string ConnectionId { get; set; }
    public string UserId { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivity { get; set; }
}
