namespace Libr4.Shared.Contracts.IntegrationEvents.Auth;

public sealed record UserLoggedInIntegrationEvent(
    Guid UserId,
    string Email,
    string? IpAddress,
    DateTimeOffset OccurredOn);

public sealed record EmailConfirmationRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTimeOffset OccurredOn);

public sealed record PasswordResetRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTimeOffset OccurredOn);
