using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Application.Dtos;
using Libr4.Chat.Domain;
using Libr4.Chat.Domain.Chats;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Application.Chats.Queries;

public record GetChatByIdQuery(Guid ChatId) : IRequest<Result<ChatDetailDto>>;

public record ChatDetailDto(
    Guid Id,
    string Title,
    ChatType Type,
    Guid? RelatedTaskId,
    DateTime CreatedAt,
    bool IsArchived,
    List<ChatMemberDto> Members);

public class GetChatByIdHandler : IRequestHandler<GetChatByIdQuery, Result<ChatDetailDto>>
{
    private readonly IChatDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetChatByIdHandler(IChatDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ChatDetailDto>> Handle(GetChatByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var chat = await _context.Chats
            .AsNoTracking()
            .Include(c => c.Members)
            .Where(c => c.Id == request.ChatId)
            .Where(c => c.Members.Any(m => m.UserId == userId))
            .Select(c => new ChatDetailDto(
                c.Id,
                c.Title,
                c.Type,
                c.RelatedTaskId,
                c.CreatedAt,
                c.IsArchived,
                c.Members.Select(m => new ChatMemberDto(
                    m.Id,
                    m.UserId,
                    m.Role,
                    m.JoinedAt,
                    m.LastReadAt
                )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (chat == null)
            return Result.Failure<ChatDetailDto>(ChatErrors.ChatNotFound);

        return Result.Success(chat);
    }
}
