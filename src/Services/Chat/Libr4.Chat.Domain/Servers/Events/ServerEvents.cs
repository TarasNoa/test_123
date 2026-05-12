using Libr4.Shared.Kernel.Domain;
using Libr4.Chat.Domain.Calls;

namespace Libr4.Chat.Domain.Servers.Events;

public sealed record ServerCreatedEvent(Guid ServerId, string Name, Guid OwnerId, DateTimeOffset CreatedAt) : DomainEvent;

public sealed record ChannelAddedEvent(Guid ServerId, Guid ChannelId, string ChannelName, ChannelType Type) : DomainEvent;

public sealed record MemberAddedEvent(Guid ServerId, Guid UserId, ServerRole Role) : DomainEvent;

public sealed record RoleCreatedEvent(Guid ServerId, Guid RoleId, string Name) : DomainEvent;

public sealed record CallScheduledEvent(Guid ServerId, Guid CallId, string Title, DateTimeOffset ScheduledAt, CallType Type) : DomainEvent;

public sealed record TaskCreatedEvent(Guid ServerId, Guid TaskId, string Title, Guid AssigneeId) : DomainEvent;

public sealed record WelcomeMessageSetEvent(Guid ServerId, string Message) : DomainEvent;

public sealed record MemberPermissionsUpdatedEvent(Guid ServerId, Guid UserId) : DomainEvent;
