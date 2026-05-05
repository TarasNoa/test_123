using Libr4.AI.Application.Abstractions;
using Libr4.AI.Domain.Chats;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.AI.Application.Chats.Queries;

public record GetMyChatsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedChatsResult>>;

public record PagedChatsResult(
    IReadOnlyList<ChatSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record ChatSummaryDto(
    Guid Id,
    string Title,
    string Model,
    AIProviderType Provider,
    AIChatStatus Status,
    DateTime CreatedAt,
    int MessageCount);

public class GetMyChatsHandler : IRequestHandler<GetMyChatsQuery, Result<PagedChatsResult>>
{
    private readonly IAIDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyChatsHandler(IAIDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedChatsResult>> Handle(GetMyChatsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var query = _context.Chats
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var chats = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ChatSummaryDto(
                c.Id,
                c.Title,
                c.Model,
                c.Provider,
                c.Status,
                c.CreatedAt,
                c.Messages.Count))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedChatsResult(chats, totalCount, request.Page, request.PageSize));
    }
}
