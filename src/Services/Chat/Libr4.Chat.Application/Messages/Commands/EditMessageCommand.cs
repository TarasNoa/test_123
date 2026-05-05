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

public record EditMessageCommand(Guid MessageId, string NewContent) : IRequest<Result>;

public class EditMessageValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.NewContent)
            .NotEmpty()
            .MaximumLength(4000);
    }
}

public class EditMessageHandler : IRequestHandler<EditMessageCommand, Result>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;

    public EditMessageHandler(
        IChatDbContext context,
        ICurrentUser currentUser,
        IEventBus eventBus)
    {
        _context = context;
        _currentUser = currentUser;
        _eventBus = eventBus;
    }

    public async Task<Result> Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken);

        if (message == null)
            return Result.Failure(ChatErrors.MessageNotFound);

        if (message.SenderId != userId)
            return Result.Failure(ChatErrors.CannotEditOthersMessage);

        message.Edit(request.NewContent);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventBus.PublishAsync(
            new MessageEdited(message.Id, message.ChatId, request.NewContent, DateTime.UtcNow),
            cancellationToken);

        return Result.Success();
    }
}
