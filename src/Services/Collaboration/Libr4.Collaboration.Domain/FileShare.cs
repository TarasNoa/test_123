namespace Libr4.Collaboration.Domain;

public class FileShare
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid UserId { get; private set; }
    public string FileName { get; private set; }
    public long FileSize { get; private set; }
    public string FileType { get; private set; }
    public string FileUrl { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FileShare() { }

    public static FileShare Create(
        Guid roomId,
        Guid userId,
        string fileName,
        long fileSize,
        string fileType,
        string fileUrl,
        string? description = null)
    {
        return new FileShare
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            UserId = userId,
            FileName = fileName,
            FileSize = fileSize,
            FileType = fileType,
            FileUrl = fileUrl,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}
