using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Projects.Events;

public sealed record ProjectCreatedEvent(Guid ProjectId, Guid OwnerId, string Title) : DomainEvent;

public sealed record ProjectPublishedEvent(Guid ProjectId, Guid OwnerId) : DomainEvent;

public sealed record MemberAddedEvent(Guid ProjectId, Guid UserId, string Role) : DomainEvent;

public sealed record MemberRemovedEvent(Guid ProjectId, Guid UserId) : DomainEvent;

public sealed record ProjectTaskCreatedEvent(Guid ProjectId, Guid TaskId, string Title) : DomainEvent;

public sealed record ProjectTaskStatusChangedEvent(Guid ProjectId, Guid TaskId, string OldStatus, string NewStatus) : DomainEvent;

public sealed record MilestoneCreatedEvent(Guid ProjectId, Guid MilestoneId, string Title) : DomainEvent;

public sealed record MilestoneCompletedEvent(Guid ProjectId, Guid MilestoneId) : DomainEvent;

public sealed record ProjectProgressUpdatedEvent(Guid ProjectId, int Progress) : DomainEvent;

public sealed record ProjectCompletedEvent(Guid ProjectId, Guid OwnerId) : DomainEvent;
