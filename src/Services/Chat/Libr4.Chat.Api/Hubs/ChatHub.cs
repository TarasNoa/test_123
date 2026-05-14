using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Libr4.Chat.Application.Abstractions;
using System.Security.Claims;
using Task = System.Threading.Tasks.Task;
using Libr4.Chat.Domain.Calls;
using Libr4.Chat.Domain.Servers;
using Libr4.Chat.Domain.Messages;

namespace Libr4.Chat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IServerService _serverService;
    private readonly ICallService _callService;
    private readonly IMediaService _mediaService;

    public ChatHub(IChatService chatService, IServerService serverService, ICallService callService, IMediaService mediaService)
    {
        _chatService = chatService;
        _serverService = serverService;
        _callService = callService;
        _mediaService = mediaService;
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Server operations
    public async Task JoinServer(Guid serverId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"server_{serverId}");
    }

    public async Task LeaveServer(Guid serverId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"server_{serverId}");
    }

    // Channel operations
    public async Task JoinChannel(Guid channelId, ChannelType type)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"channel_{channelId}");
        await Clients.Group($"channel_{channelId}").SendAsync("UserJoinedChannel", Context.UserIdentifier);
    }

    public async Task LeaveChannel(Guid channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{channelId}");
        await Clients.Group($"channel_{channelId}").SendAsync("UserLeftChannel", Context.UserIdentifier);
    }

    // Message operations
    public async Task SendMessage(Guid channelId, string content, MessageType type, List<MessageAttachmentDto>? attachments)
    {
        var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        
        var message = await _chatService.SendMessageAsync(new SendMessageRequest(channelId, content, type, attachments), userId);
        
        await Clients.Group($"channel_{channelId}").SendAsync("ReceiveMessage", message);
    }

    public async Task EditMessage(Guid messageId, string newContent)
    {
        await Clients.All.SendAsync("MessageEdited", messageId, newContent);
    }

    public async Task DeleteMessage(Guid messageId)
    {
        await Clients.All.SendAsync("MessageDeleted", messageId);
    }

    public async Task PinMessage(Guid messageId, Guid channelId)
    {
        await Clients.Group($"channel_{channelId}").SendAsync("MessagePinned", messageId);
    }

    public async Task ReactToMessage(Guid messageId, string emoji, Guid channelId)
    {
        await Clients.Group($"channel_{channelId}").SendAsync("MessageReaction", messageId, emoji, Context.UserIdentifier);
    }

    // Thread operations (like Discord threads)
    public async Task CreateThread(Guid messageId, string title)
    {
        await Clients.All.SendAsync("ThreadCreated", messageId, title);
    }

    public async Task SendThreadMessage(Guid threadId, string content)
    {
        var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await Clients.Group($"thread_{threadId}").SendAsync("ThreadMessageReceived", userId, content);
    }

    // Call operations
    public async Task InitiateCall(Guid channelId, CallType callType, string roomId)
    {
        var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        
        var call = await _callService.InitiateCallAsync(new InitiateCallRequest(channelId, callType), userId);
        var peerConnectionId = _mediaService.CreatePeerConnection(roomId);
        
        await Clients.Group($"channel_{channelId}").SendAsync("CallInitiated", call);
    }

    public async Task JoinCall(Guid callId, string roomId)
    {
        var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        
        await _callService.JoinCallAsync(callId, userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"call_{callId}");
        await Clients.Group($"call_{callId}").SendAsync("ParticipantJoined", userId);
    }

    public async Task SendOffer(Guid callId, string roomId, string offer)
    {
        var offerData = _mediaService.CreateOffer(roomId);
        await Clients.OthersInGroup($"call_{callId}").SendAsync("ReceiveOffer", offerData);
    }

    public async Task SendAnswer(Guid callId, string roomId, string answer)
    {
        _mediaService.HandleAnswer(roomId, answer);
        await Clients.OthersInGroup($"call_{callId}").SendAsync("ReceiveAnswer", answer);
    }

    public async Task SendIceCandidate(Guid callId, string roomId, string candidate)
    {
        _mediaService.AddIceCandidate(roomId, candidate);
        await Clients.OthersInGroup($"call_{callId}").SendAsync("ReceiveIceCandidate", candidate);
    }

    public async Task StartScreenShare(Guid callId)
    {
        await Clients.OthersInGroup($"call_{callId}").SendAsync("ScreenShareStarted", Context.UserIdentifier);
    }

    public async Task StopScreenShare(Guid callId)
    {
        await Clients.OthersInGroup($"call_{callId}").SendAsync("ScreenShareStopped", Context.UserIdentifier);
    }

    public async Task RecordCall(Guid callId)
    {
        await Clients.Group($"call_{callId}").SendAsync("CallRecordingStarted", callId);
    }

    public async Task EndCall(Guid callId, string roomId)
    {
        _mediaService.CloseConnection(roomId);
        await _callService.EndCallAsync(callId);
        await Clients.Group($"call_{callId}").SendAsync("CallEnded", callId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"call_{callId}");
    }

    // Voice state (like Discord)
    public async Task UpdateVoiceState(Guid channelId, bool isMuted, bool isDeafened)
    {
        await Clients.Group($"call_{channelId}").SendAsync("VoiceStateUpdated", Context.UserIdentifier, isMuted, isDeafened);
    }

    // Typing indicator
    public async Task IsTyping(Guid channelId)
    {
        await Clients.OthersInGroup($"channel_{channelId}").SendAsync("UserTyping", Context.UserIdentifier);
    }

    // Presence updates
    public async Task SetPresence(UserPresence presence)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Clients.All.SendAsync("UserPresenceUpdated", userId, presence);
    }
}

public record UserPresence(string Status, string? Activity); // Status: online, away, dnd, offline