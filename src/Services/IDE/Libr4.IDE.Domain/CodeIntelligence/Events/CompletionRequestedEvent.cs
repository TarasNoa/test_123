using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.CodeIntelligence.Events;

/// <summary>
/// Domain event raised when completion is requested
/// </summary>
public class CompletionRequestedEvent : IDomainEvent
{
    public Guid CodeIntelligenceId { get; }
    public string SessionId { get; }
    public DateTime OccurredOn { get; }
    
    public CompletionRequestedEvent(
        Guid codeIntelligenceId,
        string sessionId)
    {
        CodeIntelligenceId = codeIntelligenceId;
        SessionId = sessionId;
        OccurredOn = DateTime.UtcNow;
    }
}
