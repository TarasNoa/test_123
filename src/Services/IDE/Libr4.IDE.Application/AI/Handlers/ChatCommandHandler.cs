using Libr4.AI.Application.Abstractions;
using Libr4.AI.Infrastructure.AI;
using Libr4.IDE.Application.AI.Commands;
using Libr4.IDE.Application.AI.DTOs;
using MediatR;
using Libr4.Shared.Kernel.Results;
using System.Diagnostics;

namespace Libr4.IDE.Application.AI.Handlers;

public class ChatCommandHandler : IRequestHandler<ChatCommand, Result<AIMessageDTO>>
{
    private readonly IAIService _aiService;
    private readonly AIProviderFactory _providerFactory;

    public ChatCommandHandler(IAIService aiService, AIProviderFactory providerFactory)
    {
        _aiService = aiService;
        _providerFactory = providerFactory;
    }

    public async Task<Result<AIMessageDTO>> Handle(ChatCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        string response;
        string modelName;

        if (!string.IsNullOrEmpty(request.Provider))
        {
            var provider = _providerFactory.GetProvider(request.Provider);
            response = await provider.ChatAsync(request.Message, "You are a helpful coding assistant.", null);
            modelName = provider.ProviderName;
        }
        else
        {
            response = await _aiService.ChatAsync(request.Message, "You are a helpful coding assistant.");
            modelName = "default";
        }
        sw.Stop();

        var dto = new AIMessageDTO(
            Id: Guid.NewGuid(),
            ConversationId: request.ConversationId,
            Role: Libr4.IDE.Domain.AI.MessageRole.Assistant,
            Content: response,
            Model: modelName,
            TokensUsed: response?.Length / 4 ?? 0, // rough estimate
            ResponseTimeMs: (int)sw.ElapsedMilliseconds,
            UserRating: null,
            IsHelpful: null,
            CreatedAt: DateTime.UtcNow
        );
        return Result.Success(dto);
    }
}
