namespace Libr4.Collaboration.Domain;

public class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid UserId { get; private set; }
    public string Message { get; private set; }
    public string MessageType { get; private set; }
    public Guid? ReplyTo { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ChatMessage() { }

    public static ChatMessage Create(
        Guid roomId,
        Guid userId,
        string message,
        string messageType = "text",
        Guid? replyTo = null,
        Dictionary<string, object>? metadata = null)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            UserId = userId,
            Message = message,
            MessageType = messageType,
            ReplyTo = replyTo,
            Metadata = metadata ?? new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
