using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TaskRejection.Events;

public record ApplicationRejectedEvent(Guid RejectionId, Guid TaskId, Guid ApplicationId, Guid FreelancerId, string RejectionCategory, DateTimeOffset RejectedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
