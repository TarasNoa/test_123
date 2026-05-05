namespace Libr4.Auth.Infrastructure.Services;

public interface IRateLimitingService
{
    Task<bool> IsAllowedAsync(string key, int limit, TimeSpan period, CancellationToken ct = default);
    Task ResetAsync(string key, CancellationToken ct = default);
}

public class RateLimitingService : IRateLimitingService
{
    private readonly Dictionary<string, (int count, DateTimeOffset windowStart)> _limits = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan period, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var now = DateTimeOffset.UtcNow;
            
            if (!_limits.TryGetValue(key, out var current))
            {
                _limits[key] = (1, now);
                return true;
            }

            if (now - current.windowStart >= period)
            {
                _limits[key] = (1, now);
                return true;
            }

            if (current.count < limit)
            {
                _limits[key] = (current.count + 1, current.windowStart);
                return true;
            }

            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ResetAsync(string key, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            _limits.Remove(key);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
