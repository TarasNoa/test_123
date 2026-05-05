namespace Libr4.Shared.Contracts.IntegrationEvents.Chat;

public sealed record MessageSentIntegrationEvent(
    Guid MessageId,
    Guid ChatId,
    Guid SenderId,
    string Content,
    string MessageType,
    DateTimeOffset OccurredOn);

public sealed record DirectChatCreatedIntegrationEvent(
    Guid ChatId,
    Guid InitiatorId,
    Guid RecipientId,
    DateTimeOffset OccurredOn);
