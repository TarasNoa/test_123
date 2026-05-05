using FluentValidation;
using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain;
using Libr4.Chat.Domain.Chats;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Application.Chats.Commands;

public record JoinChatCommand(Guid ChatId) : IRequest<Result>;

public class JoinChatValidator : AbstractValidator<JoinChatCommand>
{
    public JoinChatValidator()
    {
        RuleFor(x => x.ChatId).NotEmpty();
    }
}

public class JoinChatHandler : IRequestHandler<JoinChatCommand, Result>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;

    public JoinChatHandler(IChatDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(JoinChatCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var chat = await _context.Chats
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.ChatId, cancellationToken);

        if (chat == null)
            return Result.Failure(ChatErrors.ChatNotFound);

        if (chat.IsArchived)
            return Result.Failure(ChatErrors.ChatArchived);

        if (chat.Members.Any(m => m.UserId == userId))
            return Result.Failure(ChatErrors.AlreadyMember);

        // Direct chats cannot be joined
        if (chat.Type == ChatType.Direct)
            return Result.Failure(ChatErrors.NotOwner);

        chat.AddMember(userId, ChatMemberRole.Member);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
