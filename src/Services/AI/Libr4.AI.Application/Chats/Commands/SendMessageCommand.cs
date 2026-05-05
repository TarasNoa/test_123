using FluentValidation;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Domain.Chats;
using Libr4.AI.Domain.Errors;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.AI.Application.Chats.Commands;

public record SendMessageCommand(
    Guid? ChatId,
    string Content,
    string Model = "llama2",
    AIProviderType Provider = AIProviderType.Ollama) : IRequest<Result<Guid>>;

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
        RuleFor(x => x.Model).NotEmpty();
    }
}

public class SendMessageHandler : IRequestHandler<SendMessageCommand, Result<Guid>>
{
    private readonly IAIDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILLMProviderFactory _providerFactory;

    public SendMessageHandler(
        IAIDbContext context,
        ICurrentUser currentUser,
        ILLMProviderFactory providerFactory)
    {
        _context = context;
        _currentUser = currentUser;
        _providerFactory = providerFactory;
    }

    public async Task<Result<Guid>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<Guid>(AIErrors.Unauthorized);
        var userId = _currentUser.UserId.Value;
        AIChat chat;

        if (request.ChatId.HasValue)
        {
            chat = await _context.Chats
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == request.ChatId && c.UserId == userId, cancellationToken);

            if (chat == null)
                return Result.Failure<Guid>(AIErrors.ChatNotFound);
        }
        else
        {
            // Create new chat
            chat = new AIChat(
                Guid.NewGuid(),
                userId,
                request.Content[..Math.Min(50, request.Content.Length)],
                request.Model,
                request.Provider);

            await _context.Chats.AddAsync(chat, cancellationToken);
        }

        // Add user message
        chat.AddMessage(AIChatRole.User, request.Content);

        // Get LLM response
        var provider = _providerFactory.GetProvider(request.Provider);
        var messages = chat.Messages.Select(m => new ChatMessage(
            m.Role.ToString().ToLower(),
            m.Content)).ToList();

        var llmRequest = new ChatCompletionRequest(
            request.Model,
            messages,
            Temperature: 0.7f,
            MaxTokens: 2000);

        var response = await provider.CompleteAsync(llmRequest, cancellationToken);

        if (response.IsFailure)
            return Result.Failure<Guid>(response.Error);

        // Add assistant message
        var assistantMessage = chat.AddMessage(
            AIChatRole.Assistant,
            response.Value.Content);

        assistantMessage.SetTokens(response.Value.TokensUsed);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(chat.Id);
    }
}
