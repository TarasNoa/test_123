using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TaskApproval.Events;

public record TaskApprovedEvent(Guid ApprovalId, Guid TaskId, Guid ApplicationId, Guid FreelancerId, int PaymentAmount, DateTimeOffset ApprovedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
