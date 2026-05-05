using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.ChatsCollaboration.Events;

public record InlineCommentResolvedEvent(Guid CommentId, Guid UserId, string TargetType, Guid TargetId, Guid ResolvedBy, DateTimeOffset ResolvedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
