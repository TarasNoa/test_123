using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.WebSearch.Events;

/// <summary>
/// Domain event raised when web search is started
/// </summary>
public class SearchStartedEvent : IDomainEvent
{
    public Guid WebSearchId { get; }
    public string SearchId { get; }
    public DateTime OccurredOn { get; }
    
    public SearchStartedEvent(
        Guid webSearchId,
        string searchId)
    {
        WebSearchId = webSearchId;
        SearchId = searchId;
        OccurredOn = DateTime.UtcNow;
    }
}
