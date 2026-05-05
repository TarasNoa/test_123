using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Tasks.Domain.TaskApproval.Events;

namespace Libr4.Tasks.Domain.TaskApproval;

public class TaskApproval : AggregateRoot<Guid>
{
    public Guid TaskId { get; private set; }
    public Guid ApplicationId { get; private set; }
    public Guid FreelancerId { get; private set; }
    public string ApprovalStatus { get; private set; } = string.Empty; // Pending, Approved, Rejected
    public string? ApprovalNotes { get; private set; }
    public int FinalPaymentAmount { get; private set; }
    public DateTimeOffset ApprovedAt { get; private set; }

    private TaskApproval() { }

    public void Approve(string notes, int paymentAmount, DateTimeOffset now)
    {
        ApprovalStatus = "Approved";
        ApprovalNotes = notes;
        FinalPaymentAmount = paymentAmount;
        ApprovedAt = now;
        RaiseDomainEvent(new TaskApprovedEvent(Id, TaskId, ApplicationId, FreelancerId, paymentAmount, now));
    }

    public void Reject(string notes, DateTimeOffset now)
    {
        ApprovalStatus = "Rejected";
        ApprovalNotes = notes;
        ApprovedAt = now;
        RaiseDomainEvent(new TaskApprovalRejectedEvent(Id, TaskId, ApplicationId, FreelancerId, notes, now));
    }
}
