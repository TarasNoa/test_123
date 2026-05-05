namespace Libr4.Collaboration.Domain;

public enum VideoCallStatus
{
    Active,
    Ended,
    Failed
}

public class VideoCall
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid InitiatorId { get; private set; }
    public string CallType { get; private set; }
    public Dictionary<string, object> Settings { get; private set; }
    public VideoCallStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private VideoCall() { }

    public static VideoCall Create(Guid roomId, Guid initiatorId, string callType = "video", Dictionary<string, object>? settings = null)
    {
        return new VideoCall
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            InitiatorId = initiatorId,
            CallType = callType,
            Settings = settings ?? new Dictionary<string, object>(),
            Status = VideoCallStatus.Active,
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(4) // Default 4 hour limit
        };
    }

    public void End()
    {
        Status = VideoCallStatus.Ended;
        EndedAt = DateTime.UtcNow;
    }

    public void Fail()
    {
        Status = VideoCallStatus.Failed;
        EndedAt = DateTime.UtcNow;
    }

    public bool IsActive()
    {
        return Status == VideoCallStatus.Active && DateTime.UtcNow < ExpiresAt;
    }
}
