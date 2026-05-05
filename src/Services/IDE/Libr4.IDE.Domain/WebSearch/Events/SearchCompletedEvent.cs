using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.WebSearch.Events;

/// <summary>
/// Domain event raised when web search is completed
/// </summary>
public class SearchCompletedEvent : IDomainEvent
{
    public Guid WebSearchId { get; }
    public string SearchId { get; }
    public int ResultsCount { get; }
    public DateTime OccurredOn { get; }
    
    public SearchCompletedEvent(
        Guid webSearchId,
        string searchId,
        int resultsCount)
    {
        WebSearchId = webSearchId;
        SearchId = searchId;
        ResultsCount = resultsCount;
        OccurredOn = DateTime.UtcNow;
    }
}
