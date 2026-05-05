using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;

namespace Libr4.Collaboration.Domain;

public enum RoomType
{
    Chat,
    Video,
    Whiteboard,
    Workspace,
    Document
}

public enum RoomStatus
{
    Active,
    Inactive,
    Archived,
    Deleted
}

public class CollaborationRoom : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public RoomType RoomType { get; private set; }
    public Guid CreatorId { get; private set; }
    public Guid? TaskId { get; private set; }
    public string? Description { get; private set; }
    public bool IsPublic { get; private set; }
    public int MaxParticipants { get; private set; }
    public Dictionary<string, object> Settings { get; private set; }
    public RoomStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<CollaborationSession> _sessions = new();
    public IReadOnlyCollection<CollaborationSession> Sessions => _sessions.AsReadOnly();

    private readonly List<ChatMessage> _messages = new();
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    private readonly List<FileShare> _fileShares = new();
    public IReadOnlyCollection<FileShare> FileShares => _fileShares.AsReadOnly();

    private readonly List<VideoCall> _videoCalls = new();
    public IReadOnlyCollection<VideoCall> VideoCalls => _videoCalls.AsReadOnly();

    private CollaborationRoom() { }

    public static Result<CollaborationRoom> Create(
        string name,
        RoomType roomType,
        Guid creatorId,
        Guid? taskId = null,
        string? description = null,
        bool isPublic = false,
        int maxParticipants = 50,
        Dictionary<string, object>? settings = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<CollaborationRoom>(new Error("Room.Name.Required", "Room name is required"));
        }

        if (maxParticipants <= 0 || maxParticipants > 100)
        {
            return Result.Failure<CollaborationRoom>(new Error("Room.MaxParticipants.Invalid", "Max participants must be between 1 and 100"));
        }

        var room = new CollaborationRoom
        {
            Id = Guid.NewGuid(),
            Name = name,
            RoomType = roomType,
            CreatorId = creatorId,
            TaskId = taskId,
            Description = description,
            IsPublic = isPublic,
            MaxParticipants = maxParticipants,
            Settings = settings ?? new Dictionary<string, object>(),
            Status = RoomStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add creator as owner session
        var ownerSession = CollaborationSession.Create(room.Id, creatorId, "owner");
        room._sessions.Add(ownerSession);

        // Raise domain event
        room.RaiseDomainEvent(new CollaborationRoomCreatedEvent(room.Id, creatorId, taskId));

        return room;
    }

    public Result<CollaborationSession> AddParticipant(Guid userId, string role = "participant")
    {
        if (Status != RoomStatus.Active)
        {
            return Result.Failure<CollaborationSession>(new Error("Room.NotActive", "Room is not active"));
        }

        if (_sessions.Any(s => s.UserId == userId && s.IsActive))
        {
            return Result.Failure<CollaborationSession>(new Error("Room.UserAlreadyInRoom", "User is already in the room"));
        }

        var currentParticipants = _sessions.Count(s => s.IsActive);
        if (currentParticipants >= MaxParticipants)
        {
            return Result.Failure<CollaborationSession>(new Error("Room.Full", "Room is full"));
        }

        var session = CollaborationSession.Create(Id, userId, role);
        _sessions.Add(session);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserJoinedRoomEvent(Id, userId, role));

        return session;
    }

    public Result RemoveParticipant(Guid userId)
    {
        var session = _sessions.FirstOrDefault(s => s.UserId == userId && s.IsActive);
        if (session == null)
        {
            return Result.Failure(new Error("Room.UserNotInRoom", "User is not in the room"));
        }

        session.Leave();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserLeftRoomEvent(Id, userId));

        return Result.Success();
    }

    public Result<ChatMessage> AddMessage(Guid userId, string message, string messageType = "text", Guid? replyTo = null, Dictionary<string, object>? metadata = null)
    {
        if (Status != RoomStatus.Active)
        {
            return Result.Failure<ChatMessage>(new Error("Room.NotActive", "Room is not active"));
        }

        var session = _sessions.FirstOrDefault(s => s.UserId == userId && s.IsActive);
        if (session == null)
        {
            return Result.Failure<ChatMessage>(new Error("Room.UserNotInRoom", "User is not in the room"));
        }

        var chatMessage = ChatMessage.Create(Id, userId, message, messageType, replyTo, metadata);
        _messages.Add(chatMessage);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new MessageSentEvent(Id, userId, chatMessage.Id, messageType));

        return chatMessage;
    }

    public Result<VideoCall> StartVideoCall(Guid initiatorId, string callType = "video", Dictionary<string, object>? settings = null)
    {
        if (Status != RoomStatus.Active)
        {
            return Result.Failure<VideoCall>(new Error("Room.NotActive", "Room is not active"));
        }

        var session = _sessions.FirstOrDefault(s => s.UserId == initiatorId && s.IsActive);
        if (session == null)
        {
            return Result.Failure<VideoCall>(new Error("Room.UserNotInRoom", "User is not in the room"));
        }

        if (_videoCalls.Any(v => v.Status == VideoCallStatus.Active))
        {
            return Result.Failure<VideoCall>(new Error("Room.CallInProgress", "Video call already in progress"));
        }

        var videoCall = VideoCall.Create(Id, initiatorId, callType, settings);
        _videoCalls.Add(videoCall);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new VideoCallStartedEvent(Id, initiatorId, videoCall.Id));

        return videoCall;
    }

    public Result<FileShare> ShareFile(Guid userId, string fileName, long fileSize, string fileType, string fileUrl, string? description = null)
    {
        if (Status != RoomStatus.Active)
        {
            return Result.Failure<FileShare>(new Error("Room.NotActive", "Room is not active"));
        }

        var session = _sessions.FirstOrDefault(s => s.UserId == userId && s.IsActive);
        if (session == null)
        {
            return Result.Failure<FileShare>(new Error("Room.UserNotInRoom", "User is not in the room"));
        }

        var fileShare = FileShare.Create(Id, userId, fileName, fileSize, fileType, fileUrl, description);
        _fileShares.Add(fileShare);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new FileSharedEvent(Id, userId, fileShare.Id));

        return fileShare;
    }

    public Result Close()
    {
        if (Status != RoomStatus.Active)
        {
            return Result.Failure(new Error("Room.NotActive", "Room is not active"));
        }

        Status = RoomStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;

        // Deactivate all sessions
        foreach (var session in _sessions.Where(s => s.IsActive))
        {
            session.Leave();
        }

        // End active video calls
        foreach (var videoCall in _videoCalls.Where(v => v.Status == VideoCallStatus.Active))
        {
            videoCall.End();
        }

        RaiseDomainEvent(new CollaborationRoomClosedEvent(Id, CreatorId));

        return Result.Success();
    }

    public Result Archive()
    {
        Status = RoomStatus.Archived;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new CollaborationRoomArchivedEvent(Id, CreatorId));

        return Result.Success();
    }
}

public class CollaborationRoomCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid RoomId { get; }
    public Guid CreatorId { get; }
    public Guid? TaskId { get; }

    public CollaborationRoomCreatedEvent(Guid roomId, Guid creatorId, Guid? taskId)
    {
        RoomId = roomId;
        CreatorId = creatorId;
        TaskId = taskId;
    }
}

public class UserJoinedRoomEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid RoomId { get; }
    public Guid UserId { get; }
    public string Role { get; }

    public UserJoinedRoomEvent(Guid roomId, Guid userId, string role)
    {
        RoomId = roomId;
        UserId = userId;
        Role = role;
    }
}

public class UserLeftRoomEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid RoomId { get; }
    public Guid UserId { get; }

    public UserLeftRoomEvent(Guid roomId, Guid userId)
    {
        RoomId = roomId;
        UserId = userId;
    }
}

public class MessageSentEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid RoomId { get; }
    public Guid UserId { get; }
    public Guid MessageId { get; }
    public string MessageType { get; }

    public MessageSentEvent(Guid roomId, Guid userId, Guid messageId, string messageType)
    {
        RoomId = roomId;
        UserId = userId;
        MessageId = messageId;
        MessageType = messageType;
    }
}

public class VideoCallStartedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid RoomId { get; }
    public Guid InitiatorId { get; }
    public Guid CallId { get; }

    public VideoCallStartedEvent(Guid roomId, Guid initiatorId, Guid callId)
    {
        RoomId = roomId;
        InitiatorId = initiatorId;
        CallId = callId;
    }
}

public class FileSharedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid RoomId { get; }
    public Guid UserId { get; }
    public Guid FileId { get; }

    public FileSharedEvent(Guid roomId, Guid userId, Guid fileId)
    {
        RoomId = roomId;
        UserId = userId;
        FileId = fileId;
    }
}

public class CollaborationRoomClosedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid RoomId { get; }
    public Guid CreatorId { get; }

    public CollaborationRoomClosedEvent(Guid roomId, Guid creatorId)
    {
        RoomId = roomId;
        CreatorId = creatorId;
    }
}

public class CollaborationRoomArchivedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid RoomId { get; }
    public Guid CreatorId { get; }

    public CollaborationRoomArchivedEvent(Guid roomId, Guid creatorId)
    {
        RoomId = roomId;
        CreatorId = creatorId;
    }
}
