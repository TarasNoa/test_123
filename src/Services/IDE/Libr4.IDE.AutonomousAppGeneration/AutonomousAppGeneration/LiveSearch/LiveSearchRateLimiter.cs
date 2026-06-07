using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.LiveSearch;

public sealed class LiveSearchRateLimiter
{
    private readonly LiveSearchOptions _options;
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _windows = new(StringComparer.Ordinal);

    public LiveSearchRateLimiter(IOptions<LiveSearchOptions> options) => _options = options.Value;

    public bool TryAcquire(string sessionKey)
    {
        var key = string.IsNullOrWhiteSpace(sessionKey) ? "default" : sessionKey;
        var window = _windows.GetOrAdd(key, _ => new Queue<DateTime>());
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(-1);

        lock (window)
        {
            while (window.Count > 0 && window.Peek() < cutoff)
                window.Dequeue();

            if (window.Count >= _options.MaxRequestsPerMinute)
                return false;

            window.Enqueue(now);
            return true;
        }
    }
}

public sealed class LiveSearchCache
{
    private readonly LiveSearchOptions _options;
    private readonly ConcurrentDictionary<string, (DateTime ExpiresAt, LiveSearchResponse Response)> _entries = new();

    public LiveSearchCache(IOptions<LiveSearchOptions> options) => _options = options.Value;

    public bool TryGet(string cacheKey, out LiveSearchResponse response)
    {
        response = default!;
        if (!_entries.TryGetValue(cacheKey, out var entry))
            return false;

        if (entry.ExpiresAt <= DateTime.UtcNow)
        {
            _entries.TryRemove(cacheKey, out _);
            return false;
        }

        response = entry.Response with { FromCache = true };
        return true;
    }

    public void Set(string cacheKey, LiveSearchResponse response)
    {
        var ttl = TimeSpan.FromSeconds(Math.Max(30, _options.CacheTtlSeconds));
        _entries[cacheKey] = (DateTime.UtcNow.Add(ttl), response with { FromCache = false });
    }

    public static string BuildKey(string provider, string query, int maxResults) =>
        $"{provider}:{maxResults}:{query.Trim().ToLowerInvariant()}";
}
