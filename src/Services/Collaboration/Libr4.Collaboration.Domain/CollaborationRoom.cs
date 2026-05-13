using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Collaboration.Domain.Events;

namespace Libr4.Collaboration.Domain;

public enum CollaborationRoomStatus { Active, Archived, Deleted }

public class CollaborationRoom : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CreatorId { get; private set; }
    public Guid? TaskId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public CollaborationRoomType Type { get; private set; }
    public bool IsPublic { get; private set; }
    public CollaborationRoomStatus Status { get; private set; } = CollaborationRoomStatus.Active;
    public List<Participant> Participants { get; private set; } = new();
    public List<SharedDocument> Documents { get; private set; } = new();
    public List<Whiteboard> Whiteboards { get; private set; } = new();
    public List<ChatMessage> Messages { get; private set; } = new();
    public List<CollaborationSession> Sessions { get; private set; } = new();
    public List<VideoCall> VideoCalls { get; private set; } = new();
    public List<FileShare> FileShares { get; private set; } = new();
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

    public void AddDocument(SharedDocument document)
    {
        Documents.Add(document);
        RaiseDomainEvent(new DocumentAddedEvent(Id, document.Id, document.Name));
    }

    public void CreateWhiteboard(string name)
    {
        var whiteboard = Whiteboard.Create(Id, name);
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

    public FileShare ShareFile(Guid userId, string fileName, long fileSize, string fileType, string fileUrl, string? description)
    {
        var fileShare = FileShare.Create(Id, userId, fileName, fileSize, fileType, fileUrl, description);
        FileShares.Add(fileShare);
        return fileShare;
    }
}

public enum CollaborationRoomType { Workspace, Classroom, ProjectRoom, StudyGroup, Interview }
public enum ParticipantRole { Viewer, Editor, Owner }

public record Participant(Guid UserId, ParticipantRole Role, DateTimeOffset JoinedAt);

public class CollaborationRoomSettings
{
    public bool AllowScreenShare { get; set; } = true;
    public bool AllowRecording { get; set; } = true;
    public bool RequireApprovalForJoin { get; set; } = false;
    public int MaxParticipants { get; set; } = 0; // 0 = unlimited
    public bool EnableEndToEndEncryption { get; set; } = true;
}
