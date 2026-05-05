using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.CodeIntelligence.Events;

/// <summary>
/// Domain event raised when suggestions are generated
/// </summary>
public class SuggestionsGeneratedEvent : IDomainEvent
{
    public Guid CodeIntelligenceId { get; }
    public string SessionId { get; }
    public int SuggestionsCount { get; }
    public DateTime OccurredOn { get; }
    
    public SuggestionsGeneratedEvent(
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
