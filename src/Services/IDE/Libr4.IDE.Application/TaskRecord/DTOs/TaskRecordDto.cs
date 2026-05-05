namespace Libr4.IDE.Application.TaskRecord.DTOs;

/// <summary>
/// DTO for TaskRecord
/// </summary>
public record TaskRecordDto
{
    public Guid Id { get; init; }
    public string RecordId { get; init; } = string.Empty;
    public string TaskId { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public List<TaskCheckpointDto> Checkpoints { get; init; } = new();
    public string CurrentState { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
