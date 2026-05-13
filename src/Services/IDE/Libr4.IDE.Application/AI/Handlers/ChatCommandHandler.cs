using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AI.Commands;
using Libr4.IDE.Application.AI.DTOs;
using MediatR;
using Libr4.Shared.Kernel.Results;
using System.Diagnostics;

namespace Libr4.IDE.Application.AI.Handlers;

public class ChatCommandHandler : IRequestHandler<ChatCommand, Result<AIMessageDTO>>
{
    private readonly IAIService _aiService;

    public ChatCommandHandler(IAIService aiService)
    {
        _aiService = aiService;
    }

    public async Task<Result<AIMessageDTO>> Handle(ChatCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await _aiService.ChatAsync(request.Message, "You are a helpful coding assistant.");
        sw.Stop();

        var dto = new AIMessageDTO(
            Id: Guid.NewGuid(),
            ConversationId: request.ConversationId,
            Role: Libr4.IDE.Domain.AI.MessageRole.Assistant,
            Content: response,
            Model: "default",
            TokensUsed: response?.Length / 4 ?? 0, // rough estimate
            ResponseTimeMs: (int)sw.ElapsedMilliseconds,
            UserRating: null,
            IsHelpful: null,
            CreatedAt: DateTime.UtcNow
        );
        return Result.Success(dto);
    }
}
