using Microsoft.AspNetCore.SignalR;
using Libr4.Collaboration.Application.Abstractions;
using System.Security.Claims;

namespace Libr4.Collaboration.Infrastructure.Hubs;

public class CollaborationHub : Hub
{
    private readonly ICollaborationService _service;

    public CollaborationHub(ICollaborationService service)
    {
        _service = service;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }

    // Room operations
    public async Task JoinRoom(Guid roomId)
    {
        var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");
        await Clients.Group($"room_{roomId}").SendAsync("UserJoinedRoom", userId);
    }

    public async Task LeaveRoom(Guid roomId)
    {
        var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{roomId}");
        await Clients.Group($"room_{roomId}").SendAsync("UserLeftRoom", userId);
    }

    // Document operations (real-time collaboration)
    public async Task JoinDocument(Guid documentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"document_{documentId}");
        await Clients.Group($"document_{documentId}").SendAsync("UserStartedEditing", Context.UserIdentifier);
    }

    public async Task SendDocumentChange(Guid documentId, string opType, int position, string content)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
        
        // Broadcast the change to all collaborators
        await Clients.OthersInGroup($"document_{documentId}").SendAsync("DocumentChanged", new
        {
            OpType = opType,
            Position = position,
            Content = content,
            UserId = userId,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public async Task LeaveDocument(Guid documentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"document_{documentId}");
        await Clients.Group($"document_{documentId}").SendAsync("UserStoppedEditing", Context.UserIdentifier);
    }

    // Whiteboard operations
    public async Task JoinWhiteboard(Guid whiteboardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"whiteboard_{whiteboardId}");
        await Clients.Group($"whiteboard_{whiteboardId}").SendAsync("UserJoinedWhiteboard", Context.UserIdentifier);
    }

    public async Task DrawElement(Guid whiteboardId, object elementData)
    {
        await Clients.OthersInGroup($"whiteboard_{whiteboardId}").SendAsync("ElementDrawn", elementData);
    }

    public async Task ClearWhiteboard(Guid whiteboardId)
    {
        await Clients.Group($"whiteboard_{whiteboardId}").SendAsync("WhiteboardCleared");
    }

    public async Task LeaveWhiteboard(Guid whiteboardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"whiteboard_{whiteboardId}");
        await Clients.Group($"whiteboard_{whiteboardId}").SendAsync("UserLeftWhiteboard", Context.UserIdentifier);
    }

    // Video call operations
    public async Task InitiateCall(Guid roomId, string callType, string roomConnectionId)
    {
        var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await Groups.AddToGroupAsync(Context.ConnectionId, $"call_{roomConnectionId}");
        await Clients.Group($"room_{roomId}").SendAsync("CallInitiated", new { CallId = roomConnectionId, UserId = userId, Type = callType });
    }

    public async Task JoinCall(string callRoomId)
    {
        var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await Groups.AddToGroupAsync(Context.ConnectionId, $"call_{callRoomId}");
        await Clients.OthersInGroup($"call_{callRoomId}").SendAsync("ParticipantJoined", userId);
    }

    public async Task SendOffer(string callRoomId, string offer)
    {
        await Clients.OthersInGroup($"call_{callRoomId}").SendAsync("ReceiveOffer", offer);
    }

    public async Task SendAnswer(string callRoomId, string answer)
    {
        await Clients.OthersInGroup($"call_{callRoomId}").SendAsync("ReceiveAnswer", answer);
    }

    public async Task SendIceCandidate(string callRoomId, string candidate)
    {
        await Clients.OthersInGroup($"call_{callRoomId}").SendAsync("ReceiveIceCandidate", candidate);
    }

    public async Task EndCall(string callRoomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"call_{callRoomId}");
        await Clients.Group($"call_{callRoomId}").SendAsync("CallEnded");
    }

    // Chat operations
    public async Task SendMessage(Guid roomId, string content, string messageType)
    {
        var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await Clients.Group($"room_{roomId}").SendAsync("ReceiveMessage", new
        {
            Id = Guid.NewGuid(),
            SenderId = userId,
            Content = content,
            Type = messageType,
            SentAt = DateTimeOffset.UtcNow
        });
    }

    // Presence and activity tracking
    public async Task SetUserActivity(Guid roomId, string activity)
    {
        await Clients.OthersInGroup($"room_{roomId}").SendAsync("UserActivityChanged", Context.UserIdentifier, activity);
    }

    public async Task CursorMove(Guid documentId, double x, double y)
    {
        await Clients.OthersInGroup($"document_{documentId}").SendAsync("CursorMoved", Context.UserIdentifier, x, y);
    }
}
