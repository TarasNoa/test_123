using FluentValidation;
using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain.Chats;
using ChatEntity = Libr4.Chat.Domain.Chats.Chat;
using Libr4.Chat.Domain.Chats.Events;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;

namespace Libr4.Chat.Application.Chats.Commands;

public record CreateGroupChatCommand(
    string Title,
    List<Guid> MemberIds,
    Guid? RelatedTaskId = null) : IRequest<Result<Guid>>;

public class CreateGroupChatValidator : AbstractValidator<CreateGroupChatCommand>
{
    public CreateGroupChatValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(x => x.MemberIds)
            .NotEmpty()
            .Must(x => x.Count <= 100).WithMessage("Maximum 100 members allowed");
    }
}

public class CreateGroupChatHandler : IRequestHandler<CreateGroupChatCommand, Result<Guid>>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;

    public CreateGroupChatHandler(
        IChatDbContext context,
        ICurrentUser currentUser,
        IEventBus eventBus)
    {
        _context = context;
        _currentUser = currentUser;
        _eventBus = eventBus;
    }

    public async Task<Result<Guid>> Handle(CreateGroupChatCommand request, CancellationToken cancellationToken)
    {
        var creatorId = _currentUser.UserId ?? Guid.Empty;

        var chat = ChatEntity.CreateGroup(request.Title, creatorId, request.RelatedTaskId);

        foreach (var memberId in request.MemberIds.Distinct())
        {
            if (memberId != creatorId)
                chat.AddMember(memberId, ChatRole.Member);
        }

        await _context.Chats.AddAsync(chat, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventBus.PublishAsync(
            new ChatCreated(chat.Id, creatorId, chat.Title, chat.Type),
            cancellationToken);

        return Result.Success(chat.Id);
    }
}
