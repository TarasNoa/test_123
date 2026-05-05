using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.Conversations;

public class AITemplate : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string SystemPrompt { get; private set; } = string.Empty;
    public string? UserPromptTemplate { get; private set; }
    
    // Template type
    public string Category { get; private set; } = string.Empty; // "coding", "writing", "analysis", "creative", etc.
    
    // Variables (e.g., {{task}}, {{language}}, etc.)
    public List<string> Variables { get; private set; } = new();
    
    // Usage stats
    public int UsageCount { get; private set; }
    public float AverageRating { get; private set; }
    
    // Sharing
    public bool IsPublic { get; private set; }
    public bool IsOfficial { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AITemplate() { }

    public static AITemplate Create(
        Guid userId,
        string name,
        string description,
        string systemPrompt,
        string category,
        bool isPublic = false)
    {
        return new AITemplate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description,
            SystemPrompt = systemPrompt,
            Category = category,
            IsPublic = isPublic,
            IsOfficial = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateContent(string systemPrompt, string? userPromptTemplate)
    {
        SystemPrompt = systemPrompt;
        UserPromptTemplate = userPromptTemplate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddVariable(string variable)
    {
        if (!Variables.Contains(variable))
            Variables.Add(variable);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveVariable(string variable)
    {
        Variables.Remove(variable);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordUsage(float rating)
    {
        UsageCount++;
        AverageRating = (AverageRating * (UsageCount - 1) + rating) / UsageCount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MakePublic()
    {
        IsPublic = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MakePrivate()
    {
        IsPublic = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsOfficial()
    {
        IsOfficial = true;
        IsPublic = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
