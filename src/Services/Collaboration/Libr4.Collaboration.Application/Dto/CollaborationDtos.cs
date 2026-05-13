using Libr4.Collaboration.Domain;

namespace Libr4.Collaboration.Application;

public record CreateRoomRequest(string Name, string Description, CollaborationRoomType Type);
public record ParticipantDto(Guid UserId, ParticipantRole Role, DateTimeOffset JoinedAt);
public record CollaborationRoomDto(Guid Id, string Name, string Description, Guid CreatorId, CollaborationRoomType Type, List<ParticipantDto> Participants, DateTimeOffset CreatedAt);

public record CreateDocumentRequest(Guid RoomId, string Name, string Type);
public record DocumentDto(Guid Id, string Name, string Type, Guid OwnerId, string? Content, int VersionCount, List<Guid> CollaboratingUsers);
public record UpdateDocumentRequest(Guid DocumentId, string Content);

public record CreateWhiteboardRequest(Guid RoomId, string Name);
public record DrawingElementDto(Guid Id, string Type, double X, double Y, string Color, string Data);
public record WhiteboardDto(Guid Id, string Name, List<DrawingElementDto> Elements);
public record AddDrawingElementRequest(Guid WhiteboardId, string Type, double X, double Y, double Width, double Height, string Color);

public record InitiateVideoCallRequest(Guid RoomId, VideoCallType Type);
public record CallParticipantDto(Guid UserId, string Status);
public record VideoCallDto(Guid Id, Guid RoomId, Guid InitiatorId, VideoCallType Type, string Status, List<CallParticipantDto> Participants);

public record SendChatMessageRequest(Guid RoomId, string Content, MessageType Type);
public record ChatAttachmentDto(string FileName, string Url, long Size);

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public List<ChatAttachmentDto> Attachments { get; set; } = new();
}
