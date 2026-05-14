namespace Libr4.Shared.Contracts.IntegrationEvents.Tasks;

public sealed record ApplicationAcceptedIntegrationEvent(
    Guid TaskId,
    Guid ApplicationId,
    Guid ClientId,
    Guid FreelancerId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredOn);

public sealed record TaskCompletedIntegrationEvent(
    Guid TaskId,
    Guid ClientId,
    Guid FreelancerId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredOn);
