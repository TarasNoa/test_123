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

using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Application.Chats.Commands;

public record CreateDirectChatCommand(Guid OtherUserId) : IRequest<Result<Guid>>;

public class CreateDirectChatValidator : AbstractValidator<CreateDirectChatCommand>
{
    public CreateDirectChatValidator()
    {
        RuleFor(x => x.OtherUserId).NotEmpty();
    }
}

public class CreateDirectChatHandler : IRequestHandler<CreateDirectChatCommand, Result<Guid>>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;

    public CreateDirectChatHandler(
        IChatDbContext context,
        ICurrentUser currentUser,
        IEventBus eventBus)
    {
        _context = context;
        _currentUser = currentUser;
        _eventBus = eventBus;
    }

    public async Task<Result<Guid>> Handle(CreateDirectChatCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.UserId ?? Guid.Empty;

        // Check if direct chat already exists
        var existingChat = await _context.Chats
            .AsNoTracking()
            .Where(c => c.Type == ChatType.Direct)
            .Where(c => c.Members.Any(m => m.UserId == currentUserId))
            .Where(c => c.Members.Any(m => m.UserId == request.OtherUserId))
            .Select(c => new { c.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingChat != null)
            return Result.Success(existingChat.Id);

        var chat = ChatEntity.CreateDirect(currentUserId, request.OtherUserId);

        await _context.Chats.AddAsync(chat, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventBus.PublishAsync(
            new ChatCreated(chat.Id, currentUserId, chat.Title, chat.Type),
            cancellationToken);

        return Result.Success(chat.Id);
    }
}
