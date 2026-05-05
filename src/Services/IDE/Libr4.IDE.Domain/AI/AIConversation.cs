using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;

namespace Libr4.IDE.Domain.AI;

public enum AssistantRole
{
    General,
    ProjectManager,
    Developer,
    Designer,
    Recruiter,
    Consultant
}

public enum ConversationType
{
    GeneralChat,
    TaskGeneration,
    TeamFormation,
    ProjectPlanning,
    CodeReview,
    Documentation
}

public enum MessageRole
{
    User,
    Assistant,
    System
}

public class AIConversation : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public ConversationType ConversationType { get; private set; }
    public AssistantRole AssistantRole { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Dictionary<string, object> ContextData { get; private set; }
    public string Model { get; private set; }
    public float Temperature { get; private set; }
    public int MaxTokens { get; private set; }
    public int MessagesCount { get; private set; }
    public int TokensUsed { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsPinned { get; private set; }
    public DateTime LastMessageAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<AIMessage> _messages = new();
    public IReadOnlyCollection<AIMessage> Messages => _messages.AsReadOnly();

    private readonly List<AIAction> _actions = new();
    public IReadOnlyCollection<AIAction> Actions => _actions.AsReadOnly();

    private AIConversation() { }

    public static Result<AIConversation> Create(
        Guid userId,
        string title,
        ConversationType conversationType = ConversationType.GeneralChat,
        AssistantRole assistantRole = AssistantRole.General,
        Guid? projectId = null,
        Dictionary<string, object>? contextData = null,
        string model = "gpt-4",
        float temperature = 0.7f,
        int maxTokens = 2000)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<AIConversation>(Error.Validation("Title.Required", "Title is required"));

        if (title.Length > 200)
            return Result.Failure<AIConversation>(Error.Validation("Title.TooLong", "Title cannot exceed 200 characters"));

        var conversation = new AIConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            ConversationType = conversationType,
            AssistantRole = assistantRole,
            ProjectId = projectId,
            ContextData = contextData ?? new Dictionary<string, object>(),
            Model = model,
            Temperature = temperature,
            MaxTokens = maxTokens,
            MessagesCount = 0,
            TokensUsed = 0,
            IsArchived = false,
            IsPinned = false,
            LastMessageAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        conversation.RaiseDomainEvent(new AIConversationCreatedEvent(conversation.Id, userId, title));
        return Result.Success(conversation);
    }

    public Result AddMessage(MessageRole role, string content, string? model = null, int? tokensUsed = null, int? responseTimeMs = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure(Error.Validation("Message.Required", "Message content is required"));

        var message = AIMessage.Create(Id, role, content, model, tokensUsed, responseTimeMs);
        if (message.IsFailure)
            return Result.Failure(message.Error);

        _messages.Add(message.Value);
        MessagesCount++;
        TokensUsed += tokensUsed ?? 0;
        LastMessageAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AIMessageAddedEvent(Id, message.Value.Id, role));
        return Result.Success();
    }

    public Result Archive()
    {
        if (IsArchived)
            return Result.Failure(Error.Validation("Conversation.AlreadyArchived", "Conversation is already archived"));

        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new AIConversationArchivedEvent(Id, UserId));
        return Result.Success();
    }

    public Result Pin()
    {
        if (IsPinned)
            return Result.Failure(Error.Validation("Conversation.AlreadyPinned", "Conversation is already pinned"));

        IsPinned = true;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Unpin()
    {
        if (!IsPinned)
            return Result.Failure(Error.Validation("Conversation.NotPinned", "Conversation is not pinned"));

        IsPinned = false;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result UpdateSettings(string? model = null, float? temperature = null, int? maxTokens = null)
    {
        if (model != null) Model = model;
        if (temperature.HasValue && temperature.Value >= 0 && temperature.Value <= 2) Temperature = temperature.Value;
        if (maxTokens.HasValue && maxTokens.Value > 0) MaxTokens = maxTokens.Value;

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}

public class AIMessage
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; }
    public string? Model { get; private set; }
    public int TokensUsed { get; private set; }
    public int? ResponseTimeMs { get; private set; }
    public Dictionary<string, object>? Attachments { get; private set; }
    public Dictionary<string, object>? CodeSnippets { get; private set; }
    public int? UserRating { get; private set; }
    public bool? IsHelpful { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AIMessage() { }

    public static Result<AIMessage> Create(
        Guid conversationId,
        MessageRole role,
        string content,
        string? model = null,
        int? tokensUsed = null,
        int? responseTimeMs = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure<AIMessage>(Error.Validation("Message.Required", "Message content is required"));

        return Result.Success(new AIMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            Model = model,
            TokensUsed = tokensUsed ?? 0,
            ResponseTimeMs = responseTimeMs,
            CreatedAt = DateTime.UtcNow
        });
    }

    public Result RateMessage(int rating, bool? isHelpful = null)
    {
        if (rating < 1 || rating > 5)
            return Result.Failure(Error.Validation("Message.Rating.Invalid", "Rating must be between 1 and 5"));

        UserRating = rating;
        IsHelpful = isHelpful;
        return Result.Success();
    }
}

public class AIAction
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid? MessageId { get; private set; }
    public string ActionType { get; private set; }
    public Dictionary<string, object> ActionData { get; private set; }
    public bool WasExecuted { get; private set; }
    public Dictionary<string, object>? ActionResult { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool RequiresConfirmation { get; private set; }
    public bool WasConfirmed { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExecutedAt { get; private set; }

    private AIAction() { }

    public static AIAction Create(
        Guid conversationId,
        string actionType,
        Dictionary<string, object> actionData,
        bool requiresConfirmation = true,
        Guid? messageId = null)
    {
        return new AIAction
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            MessageId = messageId,
            ActionType = actionType,
            ActionData = actionData,
            RequiresConfirmation = requiresConfirmation,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result Confirm()
    {
        if (WasConfirmed)
            return Result.Failure(Error.Validation("Action.AlreadyConfirmed", "Action already confirmed"));

        WasConfirmed = true;
        ConfirmedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Execute(Dictionary<string, object>? result = null, string? errorMessage = null)
    {
        if (!WasConfirmed && RequiresConfirmation)
            return Result.Failure(Error.Validation("Action.RequiresConfirmation", "Action requires confirmation"));

        WasExecuted = true;
        ActionResult = result;
        ErrorMessage = errorMessage;
        ExecutedAt = DateTime.UtcNow;
        return Result.Success();
    }
}

// Domain Events
public record AIConversationCreatedEvent(Guid ConversationId, Guid UserId, string Title) : DomainEvent;
public record AIMessageAddedEvent(Guid ConversationId, Guid MessageId, MessageRole Role) : DomainEvent;
public record AIConversationArchivedEvent(Guid ConversationId, Guid UserId) : DomainEvent;
