using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TaskApproval.Events;

public record TaskApprovalRejectedEvent(Guid ApprovalId, Guid TaskId, Guid ApplicationId, Guid FreelancerId, string Notes, DateTimeOffset RejectedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
