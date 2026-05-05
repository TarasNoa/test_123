using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.TaskRecord.Events;

/// <summary>
/// Domain event raised when a task is resumed
/// </summary>
public class TaskResumedEvent : IDomainEvent
{
    public Guid TaskRecordId { get; }
    public string RecordId { get; }
    public DateTime OccurredOn { get; }
    
    public TaskResumedEvent(
        Guid taskRecordId,
        string recordId)
    {
        TaskRecordId = taskRecordId;
        RecordId = recordId;
        OccurredOn = DateTime.UtcNow;
    }
}
