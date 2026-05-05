using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Reviews;

public sealed record ReviewSubmittedDomainEvent(
    Guid TaskId,
    Guid ReviewerId,
    Guid RevieweeId,
    int Rating) : DomainEvent;

public sealed record ReviewUpdatedDomainEvent(
    Guid ReviewId,
    Guid TaskId,
    Guid ReviewerId,
    Guid RevieweeId,
    int Rating) : DomainEvent;
