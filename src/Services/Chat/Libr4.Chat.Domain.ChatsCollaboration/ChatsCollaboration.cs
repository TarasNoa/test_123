using System;
using System.Collections.Generic;
using Libr4.Shared.Kernel.Domain;
using Libr4.Chat.Domain.ChatsCollaboration.Events;

namespace Libr4.Chat.Domain.ChatsCollaboration;

public enum MessageType { Text, Image, File, CodeSnippet, System, Voice, Video }
public enum CommentStatus { Active, Resolved, Deleted }
public enum QACategory { General, Technical, Design, Timeline, Budget, Communication }
public enum CollabSessionStatus { Active, Paused, Ended }
public enum QAStatus { Pending, Answered, Resolved }
public enum QAPriority { Low, Normal, High, Urgent }

public class TypingIndicator
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    public bool IsExpired() => DateTimeOffset.UtcNow > ExpiresAt;
}

public class ReadReceipt
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset ReadAt { get; set; }
    public int? ReadDurationSeconds { get; set; }
    public string? DeviceType { get; set; }
}

public class ChatMessage : AggregateRoot<Guid>
{
    public Guid ChatId { get; private set; }
    public Guid UserId { get; private set; }
    public MessageType MessageType { get; private set; } = MessageType.Text;
    public string? Content { get; private set; }
    public Dictionary<string, object>? Metadata { get; private set; }
    public bool IsEdited { get; private set; }
    public DateTimeOffset? EditedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public Dictionary<string, List<Guid>>? Reactions { get; private set; }
    public Guid? ReplyTo { get; private set; }
    public Guid? ThreadId { get; private set; }
    public List<Dictionary<string, object>>? Attachments { get; private set; }
    public List<Guid>? Mentions { get; private set; }
    public List<ReadReceipt> ReadReceipts { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ChatMessage() { }

    public bool IsThreadStarter => ThreadId.HasValue;

    public void Edit(string newContent, DateTimeOffset now)
    {
        Content = newContent;
        IsEdited = true;
        EditedAt = now;
        UpdatedAt = now;
        RaiseDomainEvent(new ChatMessageEditedEvent(Id, ChatId, UserId, now));
    }

    public void SoftDelete(DateTimeOffset now)
    {
        IsDeleted = true;
        DeletedAt = now;
        RaiseDomainEvent(new ChatMessageDeletedEvent(Id, ChatId, UserId, now));
    }

    public void Archive(DateTimeOffset now)
    {
        IsArchived = true;
        ArchivedAt = now;
        RaiseDomainEvent(new ChatMessageArchivedEvent(Id, ChatId, UserId, now));
    }
}

public class CodeSnippet
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? LineCount { get; set; }
    public int? CharacterCount { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool IsForkable { get; set; } = true;
    public bool CanExecute { get; set; }
    public Dictionary<string, object>? ExecutionResult { get; set; }
    public Guid? ForkedFrom { get; set; }
    public int ForkCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsLarge => (CharacterCount ?? 0) > 10000;
}

public class InlineComment : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string TargetType { get; private set; } = string.Empty; // deliverable, file, message, snippet
    public Guid TargetId { get; private set; }
    public string Comment { get; private set; } = string.Empty;
    public Dictionary<string, object>? Coordinates { get; private set; } // x, y, width, height
    public Dictionary<string, object>? Selection { get; private set; } // text selection
    public CommentStatus Status { get; private set; } = CommentStatus.Active;
    public bool IsResolved { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ParentId { get; private set; }
    public List<Dictionary<string, object>>? Replies { get; private set; }
    public Dictionary<string, List<Guid>>? Reactions { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private InlineComment() { }

    public bool HasCoordinates => Coordinates != null && Coordinates.Count > 0;
    public bool HasSelection => Selection != null && Selection.Count > 0;

    public void Resolve(Guid resolverId, DateTimeOffset now)
    {
        IsResolved = true;
        ResolvedBy = resolverId;
        ResolvedAt = now;
        Status = CommentStatus.Resolved;
        RaiseDomainEvent(new InlineCommentResolvedEvent(Id, UserId, TargetType, TargetId, resolverId, now));
    }
}

public class FileVersion
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public Guid UserId { get; set; }
    public int VersionNumber { get; set; }
    public string? VersionName { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? Checksum { get; set; }
    public string? ChangeDescription { get; set; }
    public Dictionary<string, object>? ChangeSummary { get; set; }
    public List<string>? Tags { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? ParentVersion { get; set; }
    public List<Guid>? MergeFrom { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public double SizeMb => FileSize / (1024.0 * 1024.0);
}

public class AnonymousQA : AggregateRoot<Guid>
{
    public Guid ProjectId { get; private set; }
    public string Question { get; private set; } = string.Empty;
    public QACategory Category { get; private set; } = QACategory.General;
    public bool IsAnonymous { get; private set; } = true;
    public Guid? UserId { get; private set; }
    public string? Answer { get; private set; }
    public Guid? AnsweredBy { get; private set; }
    public DateTimeOffset? AnsweredAt { get; set; }
    public QAStatus Status { get; private set; } = QAStatus.Pending;
    public QAPriority Priority { get; private set; } = QAPriority.Normal;
    public List<string>? Tags { get; private set; }
    public int Upvotes { get; private set; }
    public int ViewCount { get; private set; }
    public bool IsApproved { get; private set; } = true;
    public Guid? ModeratedBy { get; private set; }
    public DateTimeOffset? ModeratedAt { get; private set; }
    public string? ModerationReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AnonymousQA() { }

    public bool IsAnswered => !string.IsNullOrEmpty(Answer);
    public int DaysSinceCreated => (int)(DateTimeOffset.UtcNow - CreatedAt).TotalDays;

    public void ProvideAnswer(string answer, Guid answeredBy, DateTimeOffset now)
    {
        Answer = answer;
        AnsweredBy = answeredBy;
        AnsweredAt = now;
        Status = QAStatus.Answered;
        RaiseDomainEvent(new QAAnsweredEvent(Id, ProjectId, UserId, answeredBy, now));
    }
}

public class ChatArchive
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public int MessageCount { get; set; }
    public Dictionary<string, object>? DateRange { get; set; } // start_date, end_date
    public string ArchiveReason { get; set; } = string.Empty; // inactivity, manual, size_limit
    public Guid? ArchivedBy { get; set; }
    public string? CompressedData { get; set; }
    public string? FilePath { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? Checksum { get; set; }
    public bool IsCompressed { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTimeOffset ArchivedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
}

public class CollaborationSession : AggregateRoot<Guid>
{
    public string SessionId { get; private set; } = string.Empty;
    public List<Guid> UserIds { get; private set; } = [];
    public Guid InitiatorId { get; private set; }
    public string ContextType { get; private set; } = string.Empty; // document, whiteboard, code
    public Guid ContextId { get; private set; }
    public CollabSessionStatus Status { get; private set; } = CollabSessionStatus.Active;
    public bool IsRealTime { get; private set; } = true;
    public Dictionary<string, object>? Settings { get; private set; }
    public int? DurationSeconds { get; private set; }
    public int OperationsCount { get; private set; }
    public int ConflictsCount { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public DateTimeOffset LastActivity { get; private set; }

    private CollaborationSession() { }

    public bool IsActive => Status == CollabSessionStatus.Active && (!EndedAt.HasValue || DateTimeOffset.UtcNow <= EndedAt.Value);
    public int ParticipantCount => UserIds.Count;

    public void End(DateTimeOffset now)
    {
        Status = CollabSessionStatus.Ended;
        EndedAt = now;
        DurationSeconds = (int)(now - StartedAt).TotalSeconds;
        RaiseDomainEvent(new CollaborationSessionEndedEvent(Id, SessionId, InitiatorId, ContextType, ContextId, now));
    }
}

public class WhiteboardState
{
    public Guid Id { get; set; }
    public Guid WhiteboardId { get; set; }
    public Guid? SessionId { get; set; }
    public List<Dictionary<string, object>> Elements { get; set; } = [];
    public Dictionary<string, object>? Background { get; set; }
    public int Version { get; set; } = 1;
    public string? Checksum { get; set; }
    public Guid? ChangedBy { get; set; }
    public string? ChangeType { get; set; } // add, update, delete, move
    public Dictionary<string, object>? ChangeSummary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public int ElementCount => Elements.Count;
}

public class CollaborationOperation
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string OperationType { get; set; } = string.Empty; // insert, update, delete, move
    public Dictionary<string, object> OperationData { get; set; } = [];
    public string? ElementId { get; set; }
    public Dictionary<string, object>? Position { get; set; }
    public string Status { get; set; } = "applied"; // applied, rejected, conflicted
    public Dictionary<string, object>? ConflictResolution { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }

    public bool IsConflicted => Status == "conflicted";
}

public class SharedDocument : AggregateRoot<Guid>
{
    public Guid ChatId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = "document"; // code, whiteboard, document, spreadsheet
    public string Content { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public List<Guid> EditorsIds { get; private set; } = [];
    public List<Guid> ViewersIds { get; private set; } = [];
    public int Version { get; private set; } = 1;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private SharedDocument() { }

    public void UpdateContent(string newContent, DateTimeOffset now)
    {
        Content = newContent;
        Version++;
        UpdatedAt = now;
        RaiseDomainEvent(new SharedDocumentUpdatedEvent(Id, ChatId, OwnerId, Version, now));
    }
}
