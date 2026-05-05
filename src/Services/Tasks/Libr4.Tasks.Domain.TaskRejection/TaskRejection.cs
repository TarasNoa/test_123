using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Tasks.Domain.TaskRejection.Events;

namespace Libr4.Tasks.Domain.TaskRejection;

public class TaskRejection : AggregateRoot<Guid>
{
    public Guid TaskId { get; private set; }
    public Guid ApplicationId { get; private set; }
    public Guid FreelancerId { get; private set; }
    public string RejectionReason { get; private set; } = string.Empty;
    public string RejectionCategory { get; private set; } = string.Empty; // SkillsMismatch, Budget, Availability, Other
    public string? Feedback { get; private set; }
    public DateTimeOffset RejectedAt { get; private set; }

    private TaskRejection() { }

    public void Reject(string reason, string category, string? feedback, DateTimeOffset now)
    {
        RejectionReason = reason;
        RejectionCategory = category;
        Feedback = feedback;
        RejectedAt = now;
        RaiseDomainEvent(new ApplicationRejectedEvent(Id, TaskId, ApplicationId, FreelancerId, category, now));
    }
}
