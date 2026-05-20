using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.AI.Domain.Chats;

public enum AIChatRole
{
    System,
    User,
    Assistant,
    Tool
}

public enum AIProviderType
{
    OpenAI,
    Anthropic,
    Groq,
    OpenRouter,
    Ollama,  // Local
    Custom,
    AlibabaCloud,
    DockerModelRunner,
    Google,
    DeepSeek,
    GLM
}

public enum AIChatStatus
{
    Active,
    Paused,
    Completed,
    Error
}

public class AIChat : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public AIProviderType Provider { get; private set; }
    public AIChatStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<AIMessage> _messages = new();
    public IReadOnlyCollection<AIMessage> Messages => _messages.AsReadOnly();

    private AIChat() { } // EF Core

    public AIChat(Guid id, Guid userId, string title, string model, AIProviderType provider) : base(id)
    {
        UserId = userId;
        Title = title;
        Model = model;
        Provider = provider;
        Status = AIChatStatus.Active;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public AIMessage AddMessage(AIChatRole role, string content, string? toolCallId = null)
    {
        var message = new AIMessage(Guid.NewGuid(), Id, role, content, toolCallId);
        _messages.Add(message);
        UpdatedAt = DateTime.UtcNow;
        return message;
    }

    public void UpdateTitle(string title)
    {
        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = AIChatStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkError()
    {
        Status = AIChatStatus.Error;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class AIMessage : Entity<Guid>
{
    public Guid ChatId { get; private set; }
    public AIChatRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string? ToolCallId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int TokensUsed { get; private set; }
    public TimeSpan? GenerationTime { get; private set; }

    private AIMessage() { } // EF Core

    public AIMessage(Guid id, Guid chatId, AIChatRole role, string content, string? toolCallId = null) : base(id)
    {
        ChatId = chatId;
        Role = role;
        Content = content;
        ToolCallId = toolCallId;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetTokens(int tokens)
    {
        TokensUsed = tokens;
    }

    public void SetGenerationTime(TimeSpan time)
    {
        GenerationTime = time;
    }
}
