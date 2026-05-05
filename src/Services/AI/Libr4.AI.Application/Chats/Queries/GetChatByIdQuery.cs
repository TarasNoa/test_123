using Libr4.AI.Application.Abstractions;
using Libr4.AI.Domain.Chats;
using Libr4.AI.Domain.Errors;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.AI.Application.Chats.Queries;

public record GetChatByIdQuery(Guid ChatId) : IRequest<Result<ChatDto>>;

public record ChatDto(
    Guid Id,
    string Title,
    string Model,
    AIProviderType Provider,
    AIChatStatus Status,
    DateTime CreatedAt,
    List<MessageDto> Messages);

public record MessageDto(
    Guid Id,
    AIChatRole Role,
    string Content,
    DateTime CreatedAt,
    int TokensUsed);

public class GetChatByIdHandler : IRequestHandler<GetChatByIdQuery, Result<ChatDto>>
{
    private readonly IAIDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetChatByIdHandler(IAIDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ChatDto>> Handle(GetChatByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var chat = await _context.Chats
            .AsNoTracking()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == request.ChatId && c.UserId == userId, cancellationToken);

        if (chat == null)
            return Result.Failure<ChatDto>(AIErrors.ChatNotFound);

        var messages = chat.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(
                m.Id,
                m.Role,
                m.Content,
                m.CreatedAt,
                m.TokensUsed))
            .ToList();

        return Result.Success(new ChatDto(
            chat.Id,
            chat.Title,
            chat.Model,
            chat.Provider,
            chat.Status,
            chat.CreatedAt,
            messages));
    }
}
