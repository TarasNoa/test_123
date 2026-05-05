namespace Libr4.Collaboration.Infrastructure.Connections;

public interface IConnectionManager
{
    Task ConnectAsync(string connectionId, string userId);
    Task DisconnectAsync(string connectionId);
    Task<string?> GetUserIdAsync(string connectionId);
    Task JoinRoomAsync(string connectionId, string roomId);
    Task LeaveRoomAsync(string connectionId, string roomId);
    Task SendToRoomAsync(string roomId, object message, string? excludeConnectionId = null);
    Task SendToUserAsync(string userId, object message);
    Task SendToConnectionAsync(string connectionId, object message);
    Task BroadcastAsync(object message, string? excludeUserId = null);
    Task<ConnectionStatistics> GetStatisticsAsync();
    Task CleanupInactiveConnectionsAsync(int maxInactiveMinutes = 30);
}

public class ConnectionStatistics
{
    public int TotalConnections;
    public int ActiveConnections;
    public int TotalMessagesSent;
    public int TotalMessagesReceived;
    public int UniqueUsers;
    public int TotalRooms;
    public double UptimeSeconds;
    public double AverageMessagesPerSecond;
    public DateTime StartTime;
}
