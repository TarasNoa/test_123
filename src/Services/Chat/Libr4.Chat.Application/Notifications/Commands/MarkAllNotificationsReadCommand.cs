using Libr4.Chat.Application.Abstractions;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Application.Notifications.Commands;

public record MarkAllNotificationsReadCommand : IRequest<Result<int>>;

public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result<int>>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;

    public MarkAllNotificationsReadHandler(IChatDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<int>> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(unreadNotifications.Count);
    }
}
