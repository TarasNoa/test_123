using Libr4.Shared.Kernel.Domain;

namespace Libr4.Collaboration.Domain.Events;

public record CollaborationRoomCreatedEvent(Guid RoomId, string Name, Guid CreatorId, CollaborationRoomType Type, DateTimeOffset CreatedAt) : DomainEvent;
public record ParticipantJoinedEvent(Guid RoomId, Guid UserId, ParticipantRole Role) : DomainEvent;
public record ParticipantLeftEvent(Guid RoomId, Guid UserId) : DomainEvent;
public record DocumentAddedEvent(Guid RoomId, Guid DocumentId, string DocumentName) : DomainEvent;
public record WhiteboardCreatedEvent(Guid RoomId, Guid WhiteboardId, string Name) : DomainEvent;
public record MessageSentEvent(Guid RoomId, Guid MessageId, Guid SenderId, string Content) : DomainEvent;
public record VideoCallInitiatedEvent(Guid RoomId, Guid CallId, Guid InitiatorId, VideoCallType CallType) : DomainEvent;
public record VideoCallEndedEvent(Guid RoomId, Guid CallId) : DomainEvent;
public record RoomSettingsUpdatedEvent(Guid RoomId) : DomainEvent;
