namespace Libr4.IDE.Domain;

using Libr4.Shared.Kernel;
using Libr4.Shared.Kernel.Domain;

public class CodeSession : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Language { get; private set; } = string.Empty;
    public string ProjectId { get; private set; } = string.Empty;
    public Guid CreatorId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public bool IsActive { get; private set; }
    public List<CodeFile> Files { get; private set; } = new();
    public List<CodeSessionParticipant> Participants { get; private set; } = new();

    private CodeSession() { }

    public CodeSession(Guid id, string title, string description, string language, string projectId, Guid creatorId)
        : base(id)
    {
        Title = title;
        Description = description;
        Language = language;
        ProjectId = projectId;
        CreatorId = creatorId;
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        IsActive = true;
        
        RaiseDomainEvent(new CodeSessionCreatedEvent(Id, CreatorId));
    }

    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    public void AddFile(string fileName, string content, string language)
    {
        var file = new CodeFile(Guid.NewGuid(), Id, fileName, content, language);
        Files.Add(file);
        UpdateActivity();
    }

    public void UpdateFile(Guid fileId, string content)
    {
        var file = Files.FirstOrDefault(f => f.Id == fileId);
        if (file != null)
        {
            file.UpdateContent(content);
            UpdateActivity();
        }
    }

    public void AddParticipant(Guid userId, string role = "editor")
    {
        if (!Participants.Any(p => p.UserId == userId))
        {
            var participant = new CodeSessionParticipant(Guid.NewGuid(), Id, userId, role);
            Participants.Add(participant);
            UpdateActivity();
        }
    }

    public void RemoveParticipant(Guid userId)
    {
        var participant = Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant != null)
        {
            participant.Leave();
            UpdateActivity();
        }
    }

    public void Close()
    {
        IsActive = false;
        RaiseDomainEvent(new CodeSessionClosedEvent(Id));
    }

    public void Reopen()
    {
        IsActive = true;
        UpdateActivity();
    }
}

public class CodeFile
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string Language { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private CodeFile() { }

    public CodeFile(Guid id, Guid sessionId, string fileName, string content, string language)
    {
        Id = id;
        SessionId = sessionId;
        FileName = fileName;
        Content = content;
        Language = language;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateContent(string content)
    {
        Content = content;
        ModifiedAt = DateTime.UtcNow;
    }
}

public class CodeSessionParticipant
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = "editor";
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }
    public bool IsActive { get; private set; }

    private CodeSessionParticipant() { }

    public CodeSessionParticipant(Guid id, Guid sessionId, Guid userId, string role)
    {
        Id = id;
        SessionId = sessionId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void Leave()
    {
        LeftAt = DateTime.UtcNow;
        IsActive = false;
    }
}

// Domain Events
public record CodeSessionCreatedEvent(Guid SessionId, Guid CreatorId) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}

public record CodeSessionClosedEvent(Guid SessionId) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
