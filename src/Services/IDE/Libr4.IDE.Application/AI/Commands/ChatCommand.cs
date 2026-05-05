/*
using Libr4.IDE.Domain.AI;
using Libr4.IDE.Application.AI.DTOs;
using MediatR;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using Libr4.IDE.Application.AI.Algorithms;
using Libr4.IDE.Domain;

namespace Libr4.IDE.Application.AI.Commands;

public record ChatCommand(
    Guid UserId,
    string Message,
    Guid? ConversationId = null,
    Dictionary<string, object>? Context = null
) : IRequest<Result<ChatResponseDTO>>;

public class ChatCommandHandler : IRequestHandler<ChatCommand, Result<ChatResponseDTO>>
{
    private readonly IAIConversationRepository _conversationRepository;
    private readonly IAIAlgorithmService _algorithmService;

    public ChatCommandHandler(
        IAIConversationRepository conversationRepository,
        IAIAlgorithmService algorithmService)
    {
        _conversationRepository = conversationRepository;
        _algorithmService = algorithmService;
    }

    public async Task<Result<ChatResponseDTO>> Handle(ChatCommand request, CancellationToken cancellationToken)
    {
        // Get or create conversation
        AIConversation? conversation = null;
        if (request.ConversationId.HasValue)
        {
            conversation = await _conversationRepository.GetByIdAsync(request.ConversationId.Value);
            if (conversation == null || conversation.UserId != request.UserId)
                return Result.Failure<ChatResponseDTO>(Error.NotFound("Conversation.NotFound", "Conversation not found"));
        }
        else
        {
            var createResult = AIConversation.Create(
                request.UserId,
                request.Message.Length > 50 ? request.Message.Substring(0, 50) : request.Message,
                ConversationType.GeneralChat,
                AssistantRole.General
            );
            if (createResult.IsFailure)
                return Result.Failure<ChatResponseDTO>(createResult.Error);

            conversation = createResult.Value;
            await _conversationRepository.AddAsync(conversation);
        }

        // Add user message
        var userMessageResult = conversation.AddMessage(MessageRole.User, request.Message);
        if (userMessageResult.IsFailure)
            return Result.Failure<ChatResponseDTO>(userMessageResult.Error);

        // Get conversation history
        var messages = await _conversationRepository.GetMessagesByConversationIdAsync(conversation.Id);
        var history = messages.TakeLast(5).Select(m => new { Role = m.Role.ToString(), Content = m.Content }).ToList();

        // Detect intent and entities using F# algorithm
        var intentResult = await _algorithmService.DetectIntentAndEntitiesAsync(request.Message);
        if (intentResult.IsFailure)
            return Result.Failure<ChatResponseDTO>(intentResult.Error);

        // Generate AI response (placeholder - would integrate with actual AI service)
        var responseContent = await GenerateResponseAsync(request.Message, intentResult.Value.Intent);

        // Add assistant message
        var assistantMessageResult = conversation.AddMessage(MessageRole.Assistant, responseContent, conversation.Model);
        if (assistantMessageResult.IsFailure)
            return Result.Failure<ChatResponseDTO>(assistantMessageResult.Error);

        await _conversationRepository.UpdateAsync(conversation);

        // Score response quality using F# algorithm
        var qualityScore = await _algorithmService.ScoreResponseQualityAsync(request.Message, responseContent, intentResult.Value.Intent);

        // Build response
        var messageDto = new AIMessageDTO(
            conversation.Messages.Last().Id,
            conversation.Id,
            MessageRole.Assistant,
            responseContent,
            conversation.Model,
            responseContent.Split().Length,
            null,
            null,
            null,
            DateTime.UtcNow
        );

        var responseDto = new ChatResponseDTO(
            conversation.Id,
            messageDto,
            responseContent,
            new List<AIActionDTO>()
        );

        return Result.Success(responseDto);
    }

    private async Task<string> GenerateResponseAsync(
        string userMessage,
        Intent intent)
    {
        // Placeholder for actual AI integration
        // This would integrate with the AI service (StarCoder2-3B, Qwen2.5-3B, or external API)
        
        await Task.Delay(100); // Simulate AI processing time
        
        return $"Я понял ваш запрос о {intent}. Это демонстрационный ответ. В production будет интегрирован настоящий AI сервис.";
    }
}
*/
