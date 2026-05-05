using Libr4.IDE.Domain.AI;
using Libr4.IDE.Application.AI.DTOs;
using MediatR;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;

namespace Libr4.IDE.Application.AI.Queries;

public record GetConversationMessagesQuery(
    Guid ConversationId,
    Guid UserId
) : IRequest<Result<List<AIMessageDTO>>>;

public class GetConversationMessagesQueryHandler : IRequestHandler<GetConversationMessagesQuery, Result<List<AIMessageDTO>>>
{
    private readonly IAIConversationRepository _conversationRepository;

    public GetConversationMessagesQueryHandler(IAIConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<Result<List<AIMessageDTO>>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
        
        if (conversation == null || conversation.UserId != request.UserId)
            return Result.Failure<List<AIMessageDTO>>(Error.NotFound("Conversation.NotFound", "Conversation not found"));

        var messages = await _conversationRepository.GetMessagesByConversationIdAsync(request.ConversationId);

        var dtos = messages.Select(m => new AIMessageDTO(
            m.Id,
            m.ConversationId,
            m.Role,
            m.Content,
            m.Model,
            m.TokensUsed,
            m.ResponseTimeMs,
            m.UserRating,
            m.IsHelpful,
            m.CreatedAt
        )).ToList();

        return Result.Success(dtos);
    }
}
