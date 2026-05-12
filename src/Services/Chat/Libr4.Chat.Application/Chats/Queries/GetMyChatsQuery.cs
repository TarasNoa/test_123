using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Application.Dtos;
using ChatDto = Libr4.Chat.Application.Dtos.ChatDto;
using Libr4.Chat.Domain.Chats;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Application.Chats.Queries;

public record GetMyChatsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<ChatDto>>>;

public class GetMyChatsHandler : IRequestHandler<GetMyChatsQuery, Result<PagedResult<ChatDto>>>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyChatsHandler(IChatDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<ChatDto>>> Handle(GetMyChatsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var query = _context.Chats
            .AsNoTracking()
            .Where(c => c.Members.Any(m => m.UserId == userId))
            .Where(c => !c.IsArchived)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var chats = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ChatDto(
                c.Id,
                c.Title,
                c.Type,
                c.RelatedTaskId,
                c.CreatedAt,
                c.IsArchived,
                c.Members.Count,
                0, // Unread count - would need last read tracking
                null // LastMessage - would need separate query
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<ChatDto>(chats, totalCount, request.Page, request.PageSize));
    }
}
