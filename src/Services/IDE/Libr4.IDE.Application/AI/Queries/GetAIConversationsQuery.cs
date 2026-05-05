using Libr4.IDE.Domain.AI;
using Libr4.IDE.Application.AI.DTOs;
using MediatR;
using Libr4.Shared.Kernel.Results;

namespace Libr4.IDE.Application.AI.Queries;

public record GetAIConversationsQuery(
    Guid UserId,
    int Skip = 0,
    int Limit = 20,
    bool ArchivedOnly = false
) : IRequest<Result<List<AIConversationDTO>>>;

public class GetAIConversationsQueryHandler : IRequestHandler<GetAIConversationsQuery, Result<List<AIConversationDTO>>>
{
    private readonly IAIConversationRepository _conversationRepository;

    public GetAIConversationsQueryHandler(IAIConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<Result<List<AIConversationDTO>>> Handle(GetAIConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversations = await _conversationRepository.GetByUserIdAsync(
            request.UserId,
            request.Skip,
            request.Limit,
            request.ArchivedOnly
        );

        var dtos = conversations.Select(c => new AIConversationDTO(
            c.Id,
            c.Title,
            c.ConversationType,
            c.AssistantRole,
            c.ProjectId,
            c.Model,
            c.MessagesCount,
            c.TokensUsed,
            c.IsArchived,
            c.IsPinned,
            c.LastMessageAt,
            c.CreatedAt
        )).ToList();

        return Result.Success(dtos);
    }
}
