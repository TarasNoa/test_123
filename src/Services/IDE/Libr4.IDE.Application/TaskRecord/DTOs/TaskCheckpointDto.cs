namespace Libr4.IDE.Application.TaskRecord.DTOs;

/// <summary>
/// DTO for TaskCheckpoint
/// </summary>
public record TaskCheckpointDto
{
    public Guid Id { get; init; }
    public string CheckpointName { get; init; } = string.Empty;
    public string SerializedState { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
