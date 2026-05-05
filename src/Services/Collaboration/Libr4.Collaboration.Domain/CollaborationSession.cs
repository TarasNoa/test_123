namespace Libr4.Collaboration.Domain;

public class CollaborationSession
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }
    public bool IsActive { get; private set; }

    private CollaborationSession() { }

    public static CollaborationSession Create(Guid roomId, Guid userId, string role = "participant")
    {
        return new CollaborationSession
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Leave()
    {
        IsActive = false;
        LeftAt = DateTime.UtcNow;
    }

    public void UpdateRole(string newRole)
    {
        Role = newRole;
    }
}
