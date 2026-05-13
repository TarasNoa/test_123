using FluentValidation;
using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain;
using Libr4.Chat.Domain.Messages;
using Libr4.Chat.Domain.Messages.Events;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Application.Messages.Commands;

public record SendMessageCommand(
    Guid ChatId,
    string Content,
    MessageType Type = MessageType.Text,
    Guid? ReplyToMessageId = null,
    string? FileUrl = null,
    string? FileName = null,
    long? FileSize = null) : IRequest<Result<Guid>>;

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.ChatId).NotEmpty();
        RuleFor(x => x.Content)
            .MaximumLength(100000)
            .When(x => x.Type == MessageType.Text);
        RuleFor(x => x.FileUrl)
            .NotEmpty()
            .When(x => x.Type == MessageType.Image || x.Type == MessageType.File);
    }
}

public class SendMessageHandler : IRequestHandler<SendMessageCommand, Result<Guid>>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;

    public SendMessageHandler(
        IChatDbContext context,
        ICurrentUser currentUser,
        IEventBus eventBus)
    {
        _context = context;
        _currentUser = currentUser;
        _eventBus = eventBus;
    }

    public async Task<Result<Guid>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var senderId = _currentUser.UserId ?? Guid.Empty;

        // Verify user is member of chat
        var isMember = await _context.Chats
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.ChatId &&
                          c.Members.Any(m => m.UserId == senderId) &&
                          c.ArchivedAt == null,
                cancellationToken);

        if (!isMember)
            return Result.Failure<Guid>(ChatErrors.UserNotMember);

        var message = new Message(
            Guid.NewGuid(),
            request.ChatId,
            senderId,
            request.Content,
            request.Type,
            request.ReplyToMessageId,
            request.FileUrl,
            request.FileName,
            request.FileSize);

        await _context.Messages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventBus.PublishAsync(
            new MessageSent(
                message.Id,
                message.ChatId,
                message.SenderId,
                message.Content,
                message.Type,
                message.SentAt),
            cancellationToken);

        return Result.Success(message.Id);
    }
}
