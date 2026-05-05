using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.TaskRecord.Events;

/// <summary>
/// Domain event raised when a checkpoint is created
/// </summary>
public class CheckpointCreatedEvent : IDomainEvent
{
    public Guid TaskRecordId { get; }
    public string RecordId { get; }
    public string CheckpointName { get; }
    public DateTime OccurredOn { get; }
    
    public CheckpointCreatedEvent(
        Guid taskRecordId,
        string recordId,
        string checkpointName)
    {
        TaskRecordId = taskRecordId;
        RecordId = recordId;
        CheckpointName = checkpointName;
        OccurredOn = DateTime.UtcNow;
    }
}
