namespace Libr4.IDE.Domain.WebSearch;

/// <summary>
/// Entity representing a search result
/// </summary>
public class SearchResult
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Url { get; private set; }
    public string Snippet { get; private set; }
    public double RelevanceScore { get; private set; }
    public SearchProvider Provider { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private SearchResult() { }
    
    public SearchResult(
        string title,
        string url,
        string snippet,
        SearchProvider provider,
        double relevanceScore = 1.0)
    {
        Id = Guid.NewGuid();
        Title = title;
        Url = url;
        Snippet = snippet;
        Provider = provider;
        RelevanceScore = Math.Max(0.0, Math.Min(1.0, relevanceScore));
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetRelevanceScore(double score)
    {
        RelevanceScore = Math.Max(0.0, Math.Min(1.0, score));
    }
    
    public static SearchResult Create(
        string title,
        string url,
        string snippet,
        SearchProvider provider,
        double relevanceScore = 1.0)
    {
        return new SearchResult(title, url, snippet, provider, relevanceScore);
    }
}
