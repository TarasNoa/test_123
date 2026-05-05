using FluentValidation;
using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain;
using Libr4.Chat.Domain.Messages.Events;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Application.Messages.Commands;

public record DeleteMessageCommand(Guid MessageId) : IRequest<Result>;

public class DeleteMessageValidator : AbstractValidator<DeleteMessageCommand>
{
    public DeleteMessageValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
    }
}

public class DeleteMessageHandler : IRequestHandler<DeleteMessageCommand, Result>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;

    public DeleteMessageHandler(
        IChatDbContext context,
        ICurrentUser currentUser,
        IEventBus eventBus)
    {
        _context = context;
        _currentUser = currentUser;
        _eventBus = eventBus;
    }

    public async Task<Result> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var isAdmin = _currentUser.Roles.Contains("Admin");

        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken);

        if (message == null)
            return Result.Failure(ChatErrors.MessageNotFound);

        // Allow admin or message owner to delete
        if (message.SenderId != userId && !isAdmin)
            return Result.Failure(ChatErrors.CannotDeleteOthersMessage);

        message.SoftDelete();
        await _context.SaveChangesAsync(cancellationToken);

        await _eventBus.PublishAsync(
            new MessageDeleted(message.Id, message.ChatId, DateTime.UtcNow),
            cancellationToken);

        return Result.Success();
    }
}
