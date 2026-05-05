using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.CodeIntelligence.Events;

/// <summary>
/// Domain event raised when completion is completed
/// </summary>
public class CompletionCompletedEvent : IDomainEvent
{
    public Guid CodeIntelligenceId { get; }
    public string SessionId { get; }
    public int SuggestionsCount { get; }
    public DateTime OccurredOn { get; }
    
    public CompletionCompletedEvent(
        Guid codeIntelligenceId,
        string sessionId,
        int suggestionsCount)
    {
        CodeIntelligenceId = codeIntelligenceId;
        SessionId = sessionId;
        SuggestionsCount = suggestionsCount;
        OccurredOn = DateTime.UtcNow;
    }
}
