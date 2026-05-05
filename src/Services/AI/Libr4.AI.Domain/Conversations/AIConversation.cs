using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.Conversations;

public enum AIConversationStatus
{
    Active,
    Archived,
    Deleted
}

public enum AIIntentType
{
    Unknown,
    Question,
    TaskRequest,
    CodeHelp,
    Debugging,
    Explanation,
    Translation,
    Analysis,
    Creative,
    Other
}

public class AIConversation : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string SystemPrompt { get; private set; } = string.Empty;
    public AIConversationStatus Status { get; private set; }
    
    // RAG Context
    public string? RagContext { get; private set; }
    public List<string> RagDocuments { get; private set; } = new();
    public string? RagQuery { get; private set; }
    
    // Intent Detection
    public AIIntentType DetectedIntent { get; private set; }
    public float IntentConfidence { get; private set; }
    
    // Quality Scoring
    public float AverageQualityScore { get; private set; }
    public int TotalMessages { get; private set; }
    public int HighQualityResponses { get; private set; }
    
    // Metadata
    public string? Language { get; private set; }
    public Dictionary<string, string> Metadata { get; private set; } = new();
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<AIConversationMessage> _messages = new();
    public IReadOnlyCollection<AIConversationMessage> Messages => _messages.AsReadOnly();

    private AIConversation() { }

    public static AIConversation Create(
        Guid userId,
        string title,
        string? systemPrompt = null,
        string? language = "en")
    {
        return new AIConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            SystemPrompt = systemPrompt ?? string.Empty,
            Status = AIConversationStatus.Active,
            Language = language,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddMessage(AIConversationMessage message)
    {
        _messages.Add(message);
        TotalMessages++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateRagContext(string? context, List<string>? documents, string? query)
    {
        RagContext = context;
        RagDocuments = documents ?? new List<string>();
        RagQuery = query;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetIntent(AIIntentType intent, float confidence)
    {
        DetectedIntent = intent;
        IntentConfidence = confidence;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateQualityScore(float score)
    {
        AverageQualityScore = (AverageQualityScore * HighQualityResponses + score) / (HighQualityResponses + 1);
        if (score >= 0.7f)
            HighQualityResponses++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        Status = AIConversationStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Delete()
    {
        Status = AIConversationStatus.Deleted;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddMetadata(string key, string value)
    {
        Metadata[key] = value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public class AIConversationMessage : Entity<Guid>
{
    public Guid ConversationId { get; private set; }
    public string Role { get; private set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; private set; } = string.Empty;
    
    // RAG info
    public string? RetrievedContext { get; private set; }
    public List<string> SourceDocuments { get; private set; } = new();
    
    // Quality scoring
    public float? QualityScore { get; private set; }
    public string? QualityFeedback { get; private set; }
    
    // Intent
    public AIIntentType? DetectedIntent { get; private set; }
    
    // Performance
    public int TokensUsed { get; private set; }
    public TimeSpan? GenerationTime { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }

    private AIConversationMessage() { }

    public static AIConversationMessage Create(
        Guid conversationId,
        string role,
        string content,
        string? retrievedContext = null,
        List<string>? sourceDocuments = null)
    {
        return new AIConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            RetrievedContext = retrievedContext,
            SourceDocuments = sourceDocuments ?? new List<string>(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetQualityScore(float score, string? feedback)
    {
        QualityScore = score;
        QualityFeedback = feedback;
    }

    public void SetIntent(AIIntentType intent)
    {
        DetectedIntent = intent;
    }

    public void SetPerformance(int tokens, TimeSpan? generationTime)
    {
        TokensUsed = tokens;
        GenerationTime = generationTime;
    }
}
