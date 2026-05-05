namespace Libr4.Shared.Contracts.IntegrationEvents.Auth;

public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTimeOffset OccurredOn);
