using Libr4.IDE.Domain.AI;
using MediatR;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;

namespace Libr4.IDE.Application.AI.Commands;

public record ScoreAIMessageCommand(
    Guid MessageId,
    Guid UserId,
    int Score,
    string? Feedback = null
) : IRequest<Result<string>>;

public class ScoreAIMessageCommandHandler : IRequestHandler<ScoreAIMessageCommand, Result<string>>
{
    private readonly IAIConversationRepository _conversationRepository;

    public ScoreAIMessageCommandHandler(IAIConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public Task<Result<string>> Handle(ScoreAIMessageCommand request, CancellationToken cancellationToken)
    {
        // Validate score range
        if (request.Score < 1 || request.Score > 5)
            return Task.FromResult(Result.Failure<string>(Error.Validation("Score.Invalid", "Score must be between 1 and 5")));

        // Find message (would need to query through conversation)
        // For now, return success as placeholder
        // In production, this would update the AIMessage entity
        
        return Task.FromResult(Result.Success("Quality score submitted successfully"));
    }
}
