using System;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Collaboration.Domain;

public class SharedDocument : Entity<Guid>
{
    public Guid RoomId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty; // Code, Markdown, Rich Text, etc.
    public Guid OwnerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public List<DocumentVersion> Versions { get; private set; } = new();
    public List<Guid> CollaboratingUsers { get; private set; } = new();
    public DocumentPermissions Permissions { get; private set; } = new();

    private SharedDocument() { }

    public static SharedDocument Create(Guid roomId, string name, string type, Guid ownerId)
    {
        return new SharedDocument
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            Name = name,
            Type = type,
            OwnerId = ownerId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateContent(string newContent, Guid userId)
    {
        var version = new DocumentVersion(Guid.NewGuid(), Versions.Count + 1, Content, userId, DateTimeOffset.UtcNow);
        Versions.Add(version);
        Content = newContent;
    }

    public void AddCollaborator(Guid userId)
    {
        if (!CollaboratingUsers.Contains(userId))
        {
            CollaboratingUsers.Add(userId);
        }
    }

    public void RemoveCollaborator(Guid userId)
    {
        CollaboratingUsers.Remove(userId);
    }

    public DocumentVersion? GetVersion(int versionNumber)
    {
        return Versions.FirstOrDefault(v => v.Version == versionNumber);
    }
}

public record DocumentVersion(Guid Id, int Version, string Content, Guid AuthorId, DateTimeOffset CreatedAt);

public class DocumentPermissions
{
    public Dictionary<Guid, DocumentAccessLevel> UserPermissions { get; set; } = new();
}

public enum DocumentAccessLevel { View, Edit, Admin }