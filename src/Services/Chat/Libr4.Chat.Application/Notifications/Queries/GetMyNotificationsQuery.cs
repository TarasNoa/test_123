using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Application.Dtos;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Application.Notifications.Queries;

public record GetMyNotificationsQuery(
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<NotificationDto>>>;

public class GetMyNotificationsHandler : IRequestHandler<GetMyNotificationsQuery, Result<PagedResult<NotificationDto>>>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyNotificationsHandler(IChatDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<NotificationDto>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (request.UnreadOnly)
            query = query.Where(n => !n.IsRead);

        query = query.OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var notifications = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.Priority,
                n.IsRead,
                n.CreatedAt,
                n.ReadAt,
                n.ActionUrl,
                n.RelatedEntityId,
                n.RelatedEntityType))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<NotificationDto>(notifications, totalCount, request.Page, request.PageSize));
    }
}
