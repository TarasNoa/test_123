using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.WebSearch.Events;

/// <summary>
/// Domain event raised when search results are received
/// </summary>
public class ResultsReceivedEvent : IDomainEvent
{
    public Guid WebSearchId { get; }
    public string SearchId { get; }
    public int ResultsCount { get; }
    public DateTime OccurredOn { get; }
    
    public ResultsReceivedEvent(
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
