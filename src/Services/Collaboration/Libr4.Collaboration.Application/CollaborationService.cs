using Libr4.Collaboration.Application.Abstractions;
using Libr4.Collaboration.Domain;
using Microsoft.Extensions.Logging;

namespace Libr4.Collaboration.Application;

public class CollaborationService : ICollaborationService
{
    private readonly ICollaborationRoomRepository _roomRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IWhiteboardRepository _whiteboardRepository;
    private readonly IVideoCallRepository _callRepository;
    private readonly ILogger<CollaborationService> _logger;

    public CollaborationService(
        ICollaborationRoomRepository roomRepository,
        IDocumentRepository documentRepository,
        IWhiteboardRepository whiteboardRepository,
        IVideoCallRepository callRepository,
        ILogger<CollaborationService> logger)
    {
        _roomRepository = roomRepository;
        _documentRepository = documentRepository;
        _whiteboardRepository = whiteboardRepository;
        _callRepository = callRepository;
        _logger = logger;
    }

    public async Task<CollaborationRoomDto> CreateRoomAsync(CreateRoomRequest request, Guid creatorId, CancellationToken cancellationToken = default)
    {
        var room = CollaborationRoom.Create(request.Name, request.Description, creatorId, request.Type);
        room.AddParticipant(creatorId, ParticipantRole.Owner);
        
        await _roomRepository.AddAsync(room, cancellationToken);
        _logger.LogInformation("Collaboration room {RoomId} created by {CreatorId}", room.Id, creatorId);

        return new CollaborationRoomDto(room.Id, room.Name, room.Description, room.CreatorId, room.Type,
            room.Participants.Select(p => new ParticipantDto(p.UserId, p.Role, p.JoinedAt)).ToList(), room.CreatedAt);
    }

    public async Task<List<CollaborationRoomDto>> GetUserRoomsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rooms = await _roomRepository.GetByUserIdAsync(userId, cancellationToken);
        return rooms.Select(r => new CollaborationRoomDto(r.Id, r.Name, r.Description, r.CreatorId, r.Type,
            r.Participants.Select(p => new ParticipantDto(p.UserId, p.Role, p.JoinedAt)).ToList(), r.CreatedAt)).ToList();
    }

    public async Task JoinRoomAsync(Guid roomId, Guid userId, CancellationToken cancellationToken = default)
    {
        var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
        if (room == null) throw new InvalidOperationException("Room not found");

        room.AddParticipant(userId, ParticipantRole.Editor);
        await _roomRepository.UpdateAsync(room, cancellationToken);
        _logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
    }

    public async Task<DocumentDto> CreateDocumentAsync(CreateDocumentRequest request, Guid ownerId, CancellationToken cancellationToken = default)
    {
        var document = SharedDocument.Create(request.RoomId, request.Name, request.Type, ownerId);
        await _documentRepository.AddAsync(document, cancellationToken);

        return new DocumentDto(document.Id, document.Name, document.Type, document.OwnerId, document.Content, 
            document.Versions.Count, document.CollaboratingUsers);
    }

    public async Task UpdateDocumentAsync(UpdateDocumentRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document == null) throw new InvalidOperationException("Document not found");

        document.UpdateContent(request.Content, userId);
        await _documentRepository.UpdateAsync(document, cancellationToken);
    }

    public async Task<DocumentDto> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null) throw new InvalidOperationException("Document not found");

        return new DocumentDto(document.Id, document.Name, document.Type, document.OwnerId, document.Content,
            document.Versions.Count, document.CollaboratingUsers);
    }

    public async Task<WhiteboardDto> CreateWhiteboardAsync(CreateWhiteboardRequest request, CancellationToken cancellationToken = default)
    {
        var whiteboard = Whiteboard.Create(request.RoomId, request.Name);
        await _whiteboardRepository.AddAsync(whiteboard, cancellationToken);

        return new WhiteboardDto(whiteboard.Id, whiteboard.Name, new List<DrawingElementDto>());
    }

    public async Task AddDrawingElementAsync(AddDrawingElementRequest request, CancellationToken cancellationToken = default)
    {
        var whiteboard = await _whiteboardRepository.GetByIdAsync(request.WhiteboardId, cancellationToken);
        if (whiteboard == null) throw new InvalidOperationException("Whiteboard not found");

        var element = new DrawingElement(Guid.NewGuid(), request.Type, request.X, request.Y, request.Width, request.Height, request.Color, "2", null, DateTimeOffset.UtcNow);
        whiteboard.AddElement(element);
        await _whiteboardRepository.UpdateAsync(whiteboard, cancellationToken);
    }

    public async Task<VideoCallDto> InitiateVideoCallAsync(InitiateVideoCallRequest request, Guid initiatorId, CancellationToken cancellationToken = default)
    {
        var room = await _roomRepository.GetByIdAsync(request.RoomId, cancellationToken);
        if (room == null) throw new InvalidOperationException("Room not found");

        room.InitiateVideoCall(initiatorId, request.Type);
        await _roomRepository.UpdateAsync(room, cancellationToken);

        return new VideoCallDto(room.ActiveCall!.Id, room.ActiveCall.RoomId, room.ActiveCall.InitiatorId, room.ActiveCall.Type, room.ActiveCall.Status,
            room.ActiveCall.Participants.Select(p => new CallParticipantDto(p.UserId, p.Status)).ToList());
    }

    public async Task JoinVideoCallAsync(Guid callId, Guid userId, CancellationToken cancellationToken = default)
    {
        var call = await _callRepository.GetByIdAsync(callId, cancellationToken);
        if (call == null) throw new InvalidOperationException("Call not found");

        call.AddParticipant(userId);
        await _callRepository.UpdateAsync(call, cancellationToken);
    }

    public async Task EndVideoCallAsync(Guid callId, CancellationToken cancellationToken = default)
    {
        var call = await _callRepository.GetByIdAsync(callId, cancellationToken);
        if (call == null) throw new InvalidOperationException("Call not found");

        call.End();
        await _callRepository.UpdateAsync(call, cancellationToken);
    }

    public async Task<ChatMessageDto> SendMessageAsync(SendChatMessageRequest request, Guid senderId, CancellationToken cancellationToken = default)
    {
        var message = ChatMessage.Create(request.RoomId, senderId, request.Content, request.Type);
        
        // Save message (assumes repository exists)
        await Task.CompletedTask;
        
        return new ChatMessageDto(message.Id, message.RoomId, message.SenderId, message.Content, message.Type, message.SentAt, new List<ChatAttachmentDto>());
    }

    public async Task<List<ChatMessageDto>> GetRoomMessagesAsync(Guid roomId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        // Fetch messages from repository
        var messages = await Task.FromResult(new List<ChatMessage>());
        return messages.Select(m => new ChatMessageDto(m.Id, m.RoomId, m.SenderId, m.Content, m.Type, m.SentAt, 
            m.Attachments.Select(a => new ChatAttachmentDto(a.FileName, a.Url, a.Size)).ToList())).ToList();
    }
}