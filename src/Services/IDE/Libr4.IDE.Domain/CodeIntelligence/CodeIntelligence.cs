using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.CodeIntelligence.Events;

namespace Libr4.IDE.Domain.CodeIntelligence;

/// <summary>
/// AggregateRoot for code intelligence
/// </summary>
public class CodeIntelligence : AggregateRoot<Guid>
{
    public string SessionId { get; private set; }
    public CompletionContext Context { get; private set; }
    public List<CodeSuggestion> Suggestions { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private CodeIntelligence() { }
    
    public CodeIntelligence(
        string sessionId,
        CompletionContext context,
        List<CodeSuggestion>? suggestions = null)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        Context = context;
        Suggestions = suggestions ?? new List<CodeSuggestion>();
        Status = "initializing";
        CreatedAt = DateTime.UtcNow;
        CompletedAt = null;
    }
    
    public void AddSuggestion(CodeSuggestion suggestion)
    {
        if (suggestion != null)
        {
            Suggestions.Add(suggestion);
        }
    }
    
    public void SetStatus(string status)
    {
        Status = status;
        if (status == "completed" || status == "failed")
        {
            CompletedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Marks the completion as requested and raises a domain event
    /// </summary>
    public void MarkCompletionRequested()
    {
        AddDomainEvent(new CompletionRequestedEvent(Id, SessionId));
    }
    
    /// <summary>
    /// Marks suggestions as generated and raises a domain event
    /// </summary>
    public void MarkSuggestionsGenerated()
    {
        AddDomainEvent(new SuggestionsGeneratedEvent(Id, SessionId, Suggestions.Count));
    }
    
    /// <summary>
    /// Marks the completion as completed and raises a domain event
    /// </summary>
    public void MarkAsCompleted()
    {
        AddDomainEvent(new CompletionCompletedEvent(Id, SessionId, Suggestions.Count));
    }
    
    public static CodeIntelligence Create(
        string sessionId,
        CompletionContext context,
        List<CodeSuggestion>? suggestions = null)
    {
        return new CodeIntelligence(sessionId, context, suggestions);
    }
}
