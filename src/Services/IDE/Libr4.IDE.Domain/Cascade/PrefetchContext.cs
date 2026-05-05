namespace Libr4.IDE.Domain.Cascade;

/// <summary>
/// Value object representing web prefetch context
/// </summary>
public class WebSearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
}

/// <summary>
/// Value object for web prefetch context
/// </summary>
public class PrefetchContext
{
    public bool PrefetchEnabled { get; private set; }
    public List<WebSearchResult> WebSearchResults { get; private set; }
    public Dictionary<string, string> DocumentationReferences { get; private set; }
    public DateTime PrefetchedAt { get; private set; }
    
    private PrefetchContext() { }
    
    public PrefetchContext(
        bool prefetchEnabled,
        List<WebSearchResult> webSearchResults,
        Dictionary<string, string>? documentationReferences,
        DateTime prefetchedAt)
    {
        PrefetchEnabled = prefetchEnabled;
        WebSearchResults = webSearchResults ?? new List<WebSearchResult>();
        DocumentationReferences = documentationReferences ?? new Dictionary<string, string>();
        PrefetchedAt = prefetchedAt;
    }
    
    public void AddWebSearchResult(WebSearchResult result)
    {
        if (result != null)
        {
            WebSearchResults.Add(result);
        }
    }
    
    public void AddDocumentationReference(string key, string url)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(url))
        {
            DocumentationReferences[key] = url;
        }
    }
    
    public static PrefetchContext Create(
        bool prefetchEnabled,
        List<WebSearchResult>? webSearchResults = null,
        Dictionary<string, string>? documentationReferences = null,
        DateTime? prefetchedAt = null)
    {
        return new PrefetchContext(
            prefetchEnabled,
            webSearchResults,
            documentationReferences,
            prefetchedAt ?? DateTime.UtcNow
        );
    }
    
    public static PrefetchContext Empty => new PrefetchContext(false, new List<WebSearchResult>(), new Dictionary<string, string>(), DateTime.UtcNow);
}
