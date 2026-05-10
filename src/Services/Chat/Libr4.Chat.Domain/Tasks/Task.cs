using System;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.Tasks;

public class Task : Entity<Guid>
{
    public Guid ServerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid AssigneeId { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }

    private Task() { }

    public static Task Create(Guid serverId, string title, string description, Guid assigneeId, DateTimeOffset? dueDate)
    {
        return new Task
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            Title = title,
            Description = description,
            AssigneeId = assigneeId,
            Status = TaskStatus.Todo,
            CreatedAt = DateTimeOffset.UtcNow,
            DueDate = dueDate
        };
    }

    public void UpdateStatus(TaskStatus status)
    {
        Status = status;
    }
}

public enum TaskStatus { Todo, InProgress, Done }