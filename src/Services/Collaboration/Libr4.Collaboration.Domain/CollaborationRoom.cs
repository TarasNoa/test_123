using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Collaboration.Domain.Events;

namespace Libr4.Collaboration.Domain;

public class CollaborationRoom : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CreatorId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public CollaborationRoomType Type { get; private set; }
    public List<Participant> Participants { get; private set; } = new();
    public List<Document> Documents { get; private set; } = new();
    public List<Whiteboard> Whiteboards { get; private set; } = new();
    public List<ChatMessage> Messages { get; private set; } = new();
    public VideoCall? ActiveCall { get; private set; }
    public CollaborationRoomSettings Settings { get; private set; } = new();

    private CollaborationRoom() { }

    public static CollaborationRoom Create(string name, string description, Guid creatorId, CollaborationRoomType type)
    {
        var room = new CollaborationRoom
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatorId = creatorId,
            Type = type,
            CreatedAt = DateTimeOffset.UtcNow
        };

        room.RaiseDomainEvent(new CollaborationRoomCreatedEvent(room.Id, name, creatorId, type, room.CreatedAt));
        return room;
    }

    public void AddParticipant(Guid userId, ParticipantRole role = ParticipantRole.Editor)
    {
        if (!Participants.Any(p => p.UserId == userId))
        {
            Participants.Add(new Participant(userId, role, DateTimeOffset.UtcNow));
            RaiseDomainEvent(new ParticipantJoinedEvent(Id, userId, role));
        }
    }

    public void RemoveParticipant(Guid userId)
    {
        var participant = Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant != null)
        {
            Participants.Remove(participant);
            RaiseDomainEvent(new ParticipantLeftEvent(Id, userId));
        }
    }

    public void AddDocument(Document document)
    {
        Documents.Add(document);
        RaiseDomainEvent(new DocumentAddedEvent(Id, document.Id, document.Name));
    }

    public void CreateWhiteboard(string name)
    {
        var whiteboard = new Whiteboard(Guid.NewGuid(), name, Id);
        Whiteboards.Add(whiteboard);
        RaiseDomainEvent(new WhiteboardCreatedEvent(Id, whiteboard.Id, name));
    }

    public void AddMessage(ChatMessage message)
    {
        Messages.Add(message);
        RaiseDomainEvent(new MessageSentEvent(Id, message.Id, message.SenderId, message.Content));
    }

    public void InitiateVideoCall(Guid initiatorId, VideoCallType callType)
    {
        ActiveCall = VideoCall.Create(Id, initiatorId, callType);
        RaiseDomainEvent(new VideoCallInitiatedEvent(Id, ActiveCall.Id, initiatorId, callType));
    }

    public void EndVideoCall()
    {
        if (ActiveCall != null)
        {
            ActiveCall.End();
            RaiseDomainEvent(new VideoCallEndedEvent(Id, ActiveCall.Id));
        }
    }

    public void UpdateSettings(CollaborationRoomSettings settings)
    {
        Settings = settings;
        RaiseDomainEvent(new RoomSettingsUpdatedEvent(Id));
    }
}

public enum CollaborationRoomType { Workspace, Classroom, ProjectRoom, StudyGroup, Interview }
public enum ParticipantRole { Viewer, Editor, Owner }
public enum VideoCallType { Audio, Video }

public record Participant(Guid UserId, ParticipantRole Role, DateTimeOffset JoinedAt);

public record Document(Guid Id, string Name, string Type, Guid OwnerId, DateTimeOffset CreatedAt)
{
    public string? Content { get; set; }
    public List<DocumentVersion> Versions { get; set; } = new();
    public List<Guid> CollaboratingUsers { get; set; } = new();
}

public record DocumentVersion(Guid Id, int Version, string Content, Guid AuthorId, DateTimeOffset CreatedAt);

public record Whiteboard(Guid Id, string Name, Guid RoomId)
{
    public List<DrawingElement> Elements { get; set; } = new();
}

public record DrawingElement(Guid Id, string Type, double X, double Y, string Color, string Data);

public class CollaborationRoomSettings
{
    public bool AllowScreenShare { get; set; } = true;
    public bool AllowRecording { get; set; } = true;
    public bool RequireApprovalForJoin { get; set; } = false;
    public int MaxParticipants { get; set; } = 0; // 0 = unlimited
    public bool EnableEndToEndEncryption { get; set; } = true;
}
