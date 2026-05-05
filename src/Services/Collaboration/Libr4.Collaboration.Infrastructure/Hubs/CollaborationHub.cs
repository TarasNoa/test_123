using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Libr4.Collaboration.Infrastructure.Connections;

namespace Libr4.Collaboration.Infrastructure.Hubs;

public class CollaborationHub : Hub
{
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<CollaborationHub> _logger;

    public CollaborationHub(IConnectionManager connectionManager, ILogger<CollaborationHub> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].FirstOrDefault();
        var connectionId = Context.ConnectionId;

        if (!string.IsNullOrEmpty(userId))
        {
            await _connectionManager.ConnectAsync(connectionId, userId);
            _logger.LogInformation("WebSocket connected: {ConnectionId} for user {UserId}", connectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        await _connectionManager.DisconnectAsync(connectionId);
        _logger.LogInformation("WebSocket disconnected: {ConnectionId}", connectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(string roomId)
    {
        var connectionId = Context.ConnectionId;
        var userId = await _connectionManager.GetUserIdAsync(connectionId);

        if (userId != null)
        {
            await Groups.AddToGroupAsync(connectionId, $"room_{roomId}");
            await _connectionManager.JoinRoomAsync(connectionId, roomId);
            await Clients.Group($"room_{roomId}").SendAsync("UserJoined", new { ConnectionId = connectionId, UserId = userId });
            _logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
        }
    }

    public async Task LeaveRoom(string roomId)
    {
        var connectionId = Context.ConnectionId;
        var userId = await _connectionManager.GetUserIdAsync(connectionId);

        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(connectionId, $"room_{roomId}");
            await _connectionManager.LeaveRoomAsync(connectionId, roomId);
            await Clients.Group($"room_{roomId}").SendAsync("UserLeft", new { ConnectionId = connectionId, UserId = userId });
            _logger.LogInformation("User {UserId} left room {RoomId}", userId, roomId);
        }
    }

    public async Task SendChatMessage(string roomId, string message)
    {
        var connectionId = Context.ConnectionId;
        var userId = await _connectionManager.GetUserIdAsync(connectionId);

        if (userId != null)
        {
            await Clients.Group($"room_{roomId}").SendAsync("ChatMessage", new
            {
                RoomId = roomId,
                UserId = userId,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
            _logger.LogInformation("Chat message sent to room {RoomId} by user {UserId}", roomId, userId);
        }
    }

    public async Task SendDirectMessage(string targetConnectionId, string message)
    {
        var connectionId = Context.ConnectionId;
        var userId = await _connectionManager.GetUserIdAsync(connectionId);

        if (userId != null)
        {
            await Clients.Client(targetConnectionId).SendAsync("DirectMessage", new
            {
                FromConnectionId = connectionId,
                FromUserId = userId,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
            _logger.LogInformation("Direct message sent from {UserId} to {TargetConnectionId}", userId, targetConnectionId);
        }
    }

    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", new
        {
            ConnectionId = Context.ConnectionId,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task GetStatistics()
    {
        var stats = await _connectionManager.GetStatisticsAsync();
        await Clients.Caller.SendAsync("Statistics", stats);
    }
}
