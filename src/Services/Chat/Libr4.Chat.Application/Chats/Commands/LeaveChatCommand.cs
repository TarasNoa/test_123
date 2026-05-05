using FluentValidation;
using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain;
using Libr4.Chat.Domain.Chats.Events;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Libr4.Chat.Domain.Chats;

namespace Libr4.Chat.Application.Chats.Commands;

public record LeaveChatCommand(Guid ChatId) : IRequest<Result>;

public class LeaveChatValidator : AbstractValidator<LeaveChatCommand>
{
    public LeaveChatValidator()
    {
        RuleFor(x => x.ChatId).NotEmpty();
    }
}

public class LeaveChatHandler : IRequestHandler<LeaveChatCommand, Result>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;

    public LeaveChatHandler(
        IChatDbContext context,
        ICurrentUser currentUser,
        IEventBus eventBus)
    {
        _context = context;
        _currentUser = currentUser;
        _eventBus = eventBus;
    }

    public async Task<Result> Handle(LeaveChatCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var chat = await _context.Chats
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.ChatId, cancellationToken);

        if (chat == null)
            return Result.Failure(ChatErrors.ChatNotFound);

        if (!chat.Members.Any(m => m.UserId == userId))
            return Result.Failure(ChatErrors.UserNotMember);

        // Cannot leave direct chat
        if (chat.Type == ChatType.Direct)
            return Result.Failure(ChatErrors.NotOwner);

        chat.RemoveMember(userId);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventBus.PublishAsync(
            new MemberLeft(request.ChatId, userId),
            cancellationToken);

        return Result.Success();
    }
}
