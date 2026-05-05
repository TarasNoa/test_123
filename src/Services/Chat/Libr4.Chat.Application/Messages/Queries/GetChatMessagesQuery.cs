using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Application.Dtos;
using Libr4.Chat.Domain;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Application.Messages.Queries;

public record GetChatMessagesQuery(
    Guid ChatId,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<PagedResult<MessageDto>>>;

public class GetChatMessagesHandler : IRequestHandler<GetChatMessagesQuery, Result<PagedResult<MessageDto>>>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetChatMessagesHandler(IChatDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<MessageDto>>> Handle(
        GetChatMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        // Verify membership
        var isMember = await _context.Chats
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.ChatId && c.Members.Any(m => m.UserId == userId),
                cancellationToken);

        if (!isMember)
            return Result.Failure<PagedResult<MessageDto>>(ChatErrors.UserNotMember);

        // Update last read for user
        var member = await _context.ChatMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && 
                _context.Chats.Any(c => c.Id == request.ChatId && c.Members.Any(cm => cm.Id == m.Id)),
            cancellationToken);

        if (member != null)
        {
            member.MarkAsRead();
            await _context.SaveChangesAsync(cancellationToken);
        }

        var query = _context.Messages
            .AsNoTracking()
            .Where(m => m.ChatId == request.ChatId)
            .OrderByDescending(m => m.SentAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var messages = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MessageDto(
                m.Id,
                m.ChatId,
                m.SenderId,
                "", // Sender name would come from user service
                m.Content,
                m.Type,
                m.Status,
                m.SentAt,
                m.EditedAt,
                m.IsDeleted,
                m.FileUrl,
                m.FileName,
                m.FileSize,
                m.ReplyToMessageId))
            .ToListAsync(cancellationToken);

        // Reverse to show oldest first
        messages.Reverse();

        return Result.Success(new PagedResult<MessageDto>(messages, totalCount, request.Page, request.PageSize));
    }
}
