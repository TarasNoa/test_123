using Libr4.Shared.Kernel.Domain;
using Libr4.Social.Domain.Network;

namespace Libr4.Social.Domain.Events;

public record SocialNetworkCreatedEvent(Guid NetworkId, Guid UserId, DateTime CreatedAt) : DomainEvent;
public record ConnectionAddedEvent(Guid NetworkId, Guid ConnectedUserId, ConnectionType Type) : DomainEvent;
public record ConnectionRemovedEvent(Guid NetworkId, Guid ConnectedUserId) : DomainEvent;
public record FollowerAddedEvent(Guid NetworkId, Guid FollowerId) : DomainEvent;
public record FollowerRemovedEvent(Guid NetworkId, Guid FollowerId) : DomainEvent;
public record ProfileUpdatedEvent(Guid NetworkId, string Name, string? Bio) : DomainEvent;
public record PostCreatedEvent(Guid NetworkId, Guid PostId, string Content, List<string>? Tags) : DomainEvent;
public record PostDeletedEvent(Guid NetworkId, Guid PostId) : DomainEvent;
public record PostLikedEvent(Guid NetworkId, Guid PostId, Guid UserId) : DomainEvent;
public record PostCommentedEvent(Guid NetworkId, Guid PostId, Guid CommenterUserId, string CommentText) : DomainEvent;
public record PostSharedEvent(Guid NetworkId, Guid PostId) : DomainEvent;
