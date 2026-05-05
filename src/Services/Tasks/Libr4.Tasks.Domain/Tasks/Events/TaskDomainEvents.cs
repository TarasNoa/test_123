using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Tasks;

public sealed record TaskPublishedDomainEvent(
    Guid TaskId,
    Guid ClientId,
    string Title,
    decimal Budget,
    string Currency) : DomainEvent;

public sealed record ApplicationSubmittedDomainEvent(
    Guid TaskId,
    Guid FreelancerId,
    decimal ProposedBudget,
    string Currency) : DomainEvent;

public sealed record ApplicationAcceptedDomainEvent(
    Guid TaskId,
    Guid FreelancerId,
    decimal Budget,
    string Currency) : DomainEvent;

public sealed record TaskCompletedDomainEvent(
    Guid TaskId,
    Guid ClientId,
    Guid FreelancerId,
    decimal Budget,
    string Currency) : DomainEvent;
