using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.WebSearch.Events;

namespace Libr4.IDE.Domain.WebSearch;

/// <summary>
/// AggregateRoot for web search
/// </summary>
public class WebSearch : AggregateRoot<Guid>
{
    public string SearchId { get; private set; }
    public string Query { get; private set; }
    public List<SearchResult> Results { get; private set; }
    public List<SearchProvider> ProvidersUsed { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private WebSearch() { }
    
    public WebSearch(
        string searchId,
        string query,
        List<SearchResult>? results = null,
        List<SearchProvider>? providersUsed = null)
    {
        Id = Guid.NewGuid();
        SearchId = searchId;
        Query = query;
        Results = results ?? new List<SearchResult>();
        ProvidersUsed = providersUsed ?? new List<SearchProvider>();
        Status = "initializing";
        CreatedAt = DateTime.UtcNow;
        CompletedAt = null;
    }
    
    public void AddResult(SearchResult result)
    {
        if (result != null)
        {
            Results.Add(result);
        }
    }
    
    public void AddProviderUsed(SearchProvider provider)
    {
        if (!ProvidersUsed.Contains(provider))
        {
            ProvidersUsed.Add(provider);
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
    /// Marks the search as started and raises a domain event
    /// </summary>
    public void MarkAsStarted()
    {
        AddDomainEvent(new SearchStartedEvent(Id, SearchId));
    }
    
    /// <summary>
    /// Marks results as received and raises a domain event
    /// </summary>
    public void MarkResultsReceived()
    {
        AddDomainEvent(new ResultsReceivedEvent(Id, SearchId, Results.Count));
    }
    
    /// <summary>
    /// Marks the search as completed and raises a domain event
    /// </summary>
    public void MarkAsCompleted()
    {
        AddDomainEvent(new SearchCompletedEvent(Id, SearchId, Results.Count));
    }
    
    public static WebSearch Create(
        string searchId,
        string query,
        List<SearchResult>? results = null,
        List<SearchProvider>? providersUsed = null)
    {
        return new WebSearch(searchId, query, results, providersUsed);
    }
}
