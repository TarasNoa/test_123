namespace Libr4.Collaboration.Application.Abstractions;

public interface ICollaborationService
{
    Task<CollaborationRoomDto> CreateRoomAsync(CreateRoomRequest request, Guid creatorId, CancellationToken ct = default);
    Task<List<CollaborationRoomDto>> GetUserRoomsAsync(Guid userId, CancellationToken ct = default);
    Task JoinRoomAsync(Guid roomId, Guid userId, CancellationToken ct = default);
    Task<DocumentDto> CreateDocumentAsync(CreateDocumentRequest request, Guid ownerId, CancellationToken ct = default);
    Task UpdateDocumentAsync(UpdateDocumentRequest request, Guid userId, CancellationToken ct = default);
    Task<DocumentDto> GetDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<WhiteboardDto> CreateWhiteboardAsync(CreateWhiteboardRequest request, CancellationToken ct = default);
    Task AddDrawingElementAsync(AddDrawingElementRequest request, CancellationToken ct = default);
    Task<VideoCallDto> InitiateVideoCallAsync(InitiateVideoCallRequest request, Guid initiatorId, CancellationToken ct = default);
    Task JoinVideoCallAsync(Guid callId, Guid userId, CancellationToken ct = default);
    Task EndVideoCallAsync(Guid callId, CancellationToken ct = default);
    Task<ChatMessageDto> SendMessageAsync(SendChatMessageRequest request, Guid senderId, CancellationToken ct = default);
    Task<List<ChatMessageDto>> GetRoomMessagesAsync(Guid roomId, int skip = 0, int take = 50, CancellationToken ct = default);
}
