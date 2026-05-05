using Libr4.IDE.Domain.AI;
using Libr4.IDE.Application.AI.DTOs;
using MediatR;
using Libr4.Shared.Kernel.Results;

namespace Libr4.IDE.Application.AI.Commands;

public record CreateAIConversationCommand(
    Guid UserId,
    string Title,
    ConversationType ConversationType = ConversationType.GeneralChat,
    AssistantRole AssistantRole = AssistantRole.General,
    Guid? ProjectId = null,
    Dictionary<string, object>? ContextData = null,
    string Model = "gpt-4",
    float Temperature = 0.7f,
    int MaxTokens = 2000
) : IRequest<Result<AIConversationDTO>>;

public class CreateAIConversationCommandHandler : IRequestHandler<CreateAIConversationCommand, Result<AIConversationDTO>>
{
    private readonly IAIConversationRepository _conversationRepository;

    public CreateAIConversationCommandHandler(IAIConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<Result<AIConversationDTO>> Handle(CreateAIConversationCommand request, CancellationToken cancellationToken)
    {
        var conversationResult = AIConversation.Create(
            request.UserId,
            request.Title,
            request.ConversationType,
            request.AssistantRole,
            request.ProjectId,
            request.ContextData,
            request.Model,
            request.Temperature,
            request.MaxTokens
        );

        if (conversationResult.IsFailure)
            return Result.Failure<AIConversationDTO>(conversationResult.Error);

        await _conversationRepository.AddAsync(conversationResult.Value);

        var dto = new AIConversationDTO(
            conversationResult.Value.Id,
            conversationResult.Value.Title,
            conversationResult.Value.ConversationType,
            conversationResult.Value.AssistantRole,
            conversationResult.Value.ProjectId,
            conversationResult.Value.Model,
            conversationResult.Value.MessagesCount,
            conversationResult.Value.TokensUsed,
            conversationResult.Value.IsArchived,
            conversationResult.Value.IsPinned,
            conversationResult.Value.LastMessageAt,
            conversationResult.Value.CreatedAt
        );

        return Result.Success(dto);
    }
}
