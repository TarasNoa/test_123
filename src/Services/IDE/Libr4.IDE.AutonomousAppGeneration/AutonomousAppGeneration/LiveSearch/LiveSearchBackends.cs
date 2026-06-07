using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.LiveSearch;

public sealed class DuckDuckGoLiveSearchBackend : ILiveWebSearchBackend
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<DuckDuckGoLiveSearchBackend> _logger;

    public DuckDuckGoLiveSearchBackend(HttpClient http, ILogger<DuckDuckGoLiveSearchBackend> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string ProviderName => "duckduckgo";

    public async Task<IReadOnlyList<LiveSearchHit>> SearchAsync(string query, int maxResults, CancellationToken ct)
    {
        var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_redirect=1&no_html=1&skip_disambig=1";
        using var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        var hits = new List<LiveSearchHit>();
        var root = doc.RootElement;
        if (root.TryGetProperty("AbstractText", out var abs) && abs.ValueKind == JsonValueKind.String)
        {
            var text = abs.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var title = root.TryGetProperty("Heading", out var heading) ? heading.GetString() ?? query : query;
                var link = root.TryGetProperty("AbstractURL", out var abstractUrl) ? abstractUrl.GetString() ?? string.Empty : string.Empty;
                hits.Add(new LiveSearchHit(title!, link!, text!, ProviderName));
            }
        }

        if (root.TryGetProperty("RelatedTopics", out var related) && related.ValueKind == JsonValueKind.Array)
            AppendRelatedTopics(related, hits, maxResults);

        return hits.Take(maxResults).ToList();
    }

    private static void AppendRelatedTopics(JsonElement related, List<LiveSearchHit> hits, int maxResults)
    {
        foreach (var item in related.EnumerateArray())
        {
            if (hits.Count >= maxResults)
                break;

            if (item.TryGetProperty("Topics", out var topics) && topics.ValueKind == JsonValueKind.Array)
            {
                AppendRelatedTopics(topics, hits, maxResults);
                continue;
            }

            if (!item.TryGetProperty("Text", out var textEl) || textEl.ValueKind != JsonValueKind.String)
                continue;

            var text = textEl.GetString() ?? string.Empty;
            var url = item.TryGetProperty("FirstURL", out var urlEl) && urlEl.ValueKind == JsonValueKind.String
                ? urlEl.GetString() ?? string.Empty
                : string.Empty;
            var title = text.Split('-', 2)[0].Trim();
            hits.Add(new LiveSearchHit(title, url, text, "duckduckgo"));
        }
    }
}

public sealed class BraveLiveSearchBackend : ILiveWebSearchBackend
{
    private readonly HttpClient _http;
    private readonly LiveSearchOptions _options;
    private readonly ILogger<BraveLiveSearchBackend> _logger;

    public BraveLiveSearchBackend(
        HttpClient http,
        IOptions<LiveSearchOptions> options,
        ILogger<BraveLiveSearchBackend> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "brave";

    public async Task<IReadOnlyList<LiveSearchHit>> SearchAsync(string query, int maxResults, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.BraveApiKey))
            throw new InvalidOperationException("brave_api_key_missing");

        var url = $"https://api.search.brave.com/res/v1/web/search?q={Uri.EscapeDataString(query)}&count={maxResults}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-Subscription-Token", _options.BraveApiKey);

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        var hits = new List<LiveSearchHit>();
        if (!doc.RootElement.TryGetProperty("web", out var web)
            || !web.TryGetProperty("results", out var results)
            || results.ValueKind != JsonValueKind.Array)
            return hits;

        foreach (var item in results.EnumerateArray())
        {
            var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? query : query;
            var link = item.TryGetProperty("url", out var u) ? u.GetString() ?? string.Empty : string.Empty;
            var snippet = item.TryGetProperty("description", out var d) ? d.GetString() ?? string.Empty : string.Empty;
            hits.Add(new LiveSearchHit(title, link, snippet, ProviderName));
            if (hits.Count >= maxResults)
                break;
        }

        return hits;
    }
}

public sealed class XLiveSearchBackend : ILiveXSearchBackend
{
    private readonly HttpClient _http;
    private readonly LiveSearchOptions _options;
    private readonly ILogger<XLiveSearchBackend> _logger;

    public XLiveSearchBackend(
        HttpClient http,
        IOptions<LiveSearchOptions> options,
        ILogger<XLiveSearchBackend> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LiveSearchHit>> SearchAsync(string query, int maxResults, CancellationToken ct)
    {
        if (!_options.EnableSearchX || string.IsNullOrWhiteSpace(_options.XApiBearerToken))
            throw new InvalidOperationException("search_x_disabled");

        var url =
            $"https://api.twitter.com/2/tweets/search/recent?query={Uri.EscapeDataString(query)}&max_results={Math.Clamp(maxResults, 10, 100)}&tweet.fields=created_at,author_id";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.XApiBearerToken);

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        var hits = new List<LiveSearchHit>();
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return hits;

        foreach (var tweet in data.EnumerateArray())
        {
            var id = tweet.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
            var text = tweet.TryGetProperty("text", out var textEl) ? textEl.GetString() ?? string.Empty : string.Empty;
            hits.Add(new LiveSearchHit(
                $"tweet:{id}",
                $"https://x.com/i/web/status/{id}",
                text,
                "x"));
            if (hits.Count >= maxResults)
                break;
        }

        return hits;
    }
}
