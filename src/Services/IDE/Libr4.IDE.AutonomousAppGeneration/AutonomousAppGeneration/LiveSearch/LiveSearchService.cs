using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.LiveSearch;

public sealed class LiveSearchService : ILiveSearchService
{
    private readonly LiveSearchOptions _options;
    private readonly LiveSearchRateLimiter _rateLimiter;
    private readonly LiveSearchCache _cache;
    private readonly DuckDuckGoLiveSearchBackend _duckDuckGo;
    private readonly BraveLiveSearchBackend _brave;
    private readonly XLiveSearchBackend _xSearch;
    private readonly ILogger<LiveSearchService> _logger;

    public LiveSearchService(
        IOptions<LiveSearchOptions> options,
        LiveSearchRateLimiter rateLimiter,
        LiveSearchCache cache,
        DuckDuckGoLiveSearchBackend duckDuckGo,
        BraveLiveSearchBackend brave,
        XLiveSearchBackend xSearch,
        ILogger<LiveSearchService> logger)
    {
        _options = options.Value;
        _rateLimiter = rateLimiter;
        _cache = cache;
        _duckDuckGo = duckDuckGo;
        _brave = brave;
        _xSearch = xSearch;
        _logger = logger;
    }

    public Task<LiveSearchResponse> SearchWebAsync(LiveSearchRequest request, CancellationToken ct = default) =>
        SearchInternalAsync(
            request,
            provider: ResolveWebProvider(),
            backend: ResolveWebProvider() == "brave" ? _brave : _duckDuckGo,
            ct);

    public Task<LiveSearchResponse> SearchXAsync(LiveSearchRequest request, CancellationToken ct = default) =>
        SearchXInternalAsync(request, ct);

    private async Task<LiveSearchResponse> SearchInternalAsync(
        LiveSearchRequest request,
        string provider,
        ILiveWebSearchBackend backend,
        CancellationToken ct)
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("live_search_disabled");

        ValidateRequest(request);

        var maxResults = Math.Clamp(request.MaxResults ?? _options.MaxResults, 1, 20);
        var cacheKey = LiveSearchCache.BuildKey(provider, request.Query, maxResults);
        if (_cache.TryGet(cacheKey, out var cached))
            return cached;

        if (!_rateLimiter.TryAcquire(request.SessionKey ?? "web"))
            throw new InvalidOperationException("live_search_rate_limited");

        var hits = await backend.SearchAsync(request.Query, maxResults, ct).ConfigureAwait(false);
        hits = hits.Where(hit => IsHitUrlAllowed(hit)).ToList();

        var response = SearchContentTruncator.Truncate(
            new LiveSearchResponse(request.Query, provider, false, hits),
            _options.MaxSnippetChars,
            _options.MaxResponseChars);

        _cache.Set(cacheKey, response);
        _logger.LogInformation("Live web search `{Query}` via {Provider} -> {Count} hit(s)", request.Query, provider, hits.Count);
        return response;
    }

    private async Task<LiveSearchResponse> SearchXInternalAsync(LiveSearchRequest request, CancellationToken ct)
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("live_search_disabled");

        if (!_options.EnableSearchX || string.IsNullOrWhiteSpace(_options.XApiBearerToken))
            throw new InvalidOperationException("search_x_disabled");

        ValidateRequest(request);

        var maxResults = Math.Clamp(request.MaxResults ?? _options.MaxResults, 1, 20);
        var provider = "x";
        var cacheKey = LiveSearchCache.BuildKey(provider, request.Query, maxResults);
        if (_cache.TryGet(cacheKey, out var cached))
            return cached;

        if (!_rateLimiter.TryAcquire(request.SessionKey ?? "x"))
            throw new InvalidOperationException("live_search_rate_limited");

        var hits = await _xSearch.SearchAsync(request.Query, maxResults, ct).ConfigureAwait(false);
        var response = SearchContentTruncator.Truncate(
            new LiveSearchResponse(request.Query, provider, false, hits),
            _options.MaxSnippetChars,
            _options.MaxResponseChars);

        _cache.Set(cacheKey, response);
        return response;
    }

    private void ValidateRequest(LiveSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            throw new InvalidOperationException("query_required");

        if (_options.BlockPrivateNetworkTargets && !SearchSsrfGuard.IsQuerySafe(request.Query))
            throw new InvalidOperationException("query_blocked_by_ssrf_policy");
    }

    private string ResolveWebProvider()
    {
        if (string.Equals(_options.DefaultWebProvider, "brave", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_options.BraveApiKey))
            return "brave";

        return "duckduckgo";
    }

    private static bool IsHitUrlAllowed(LiveSearchHit hit)
    {
        if (string.IsNullOrWhiteSpace(hit.Url))
            return true;

        return Uri.TryCreate(hit.Url, UriKind.Absolute, out var uri) && !SearchSsrfGuard.IsBlockedTarget(uri);
    }
}
