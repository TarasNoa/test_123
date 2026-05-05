using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Chat.Domain.RealtimeCollaboration.Events;

namespace Libr4.Chat.Domain.RealtimeCollaboration;

public enum OperationType
{
    Insert,
    Delete,
    Update
}

public enum ConflictResolution
{
    ClientWins,
    ServerWins,
    Merge
}

public class CollaborativeDocument : AggregateRoot<Guid>
{
    public string DocumentName { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateTimeOffset LastModifiedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CollaborativeDocument() { }

    public void ApplyOperation(OperationType opType, string content, Guid userId, DateTimeOffset now)
    {
        Version++;
        
        switch (opType)
        {
            case OperationType.Insert:
                Content += content;
                break;
            case OperationType.Delete:
                Content = Content.Replace(content, "");
                break;
            case OperationType.Update:
                Content = content;
                break;
        }
        
        LastModifiedAt = now;
        RaiseDomainEvent(new DocumentOperationAppliedEvent(Id, DocumentName, opType, userId, Version, now));
    }

    public void ResolveConflict(ConflictResolution resolution, Guid userId, DateTimeOffset now)
    {
        Version++;
        LastModifiedAt = now;
        RaiseDomainEvent(new ConflictResolvedEvent(Id, DocumentName, resolution, userId, now));
    }
}

public class DocumentOperation
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public OperationType OpType { get; set; }
    public string? Content { get; set; }
    public int Position { get; set; }
    public int Version { get; set; }
    public DateTimeOffset AppliedAt { get; set; }
}

public class ConflictEvent
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid Operation1Id { get; set; }
    public Guid Operation2Id { get; set; }
    public ConflictResolution Resolution { get; set; }
    public DateTimeOffset ResolvedAt { get; set; }
}
