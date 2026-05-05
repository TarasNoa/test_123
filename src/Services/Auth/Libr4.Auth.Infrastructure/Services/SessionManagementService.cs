namespace Libr4.Auth.Infrastructure.Services;

public class UserSession
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive => !ExpiresAt.HasValue || ExpiresAt > DateTimeOffset.UtcNow;
}

public interface ISessionManagementService
{
    Task<Guid> CreateSessionAsync(Guid userId, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
    Task<UserSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task UpdateActivityAsync(Guid sessionId, CancellationToken ct = default);
    Task TerminateSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task TerminateAllUserSessionsAsync(Guid userId, CancellationToken ct = default);
    Task CleanupExpiredSessionsAsync(CancellationToken ct = default);
}

public class SessionManagementService : ISessionManagementService
{
    private readonly Dictionary<Guid, UserSession> _sessions = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<Guid> CreateSessionAsync(Guid userId, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var session = new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTimeOffset.UtcNow,
                LastActivityAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(30) // Default 30 days
            };
            
            _sessions[session.SessionId] = session;
            return session.SessionId;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<UserSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                if (session.IsActive)
                {
                    return session;
                }
                _sessions.Remove(sessionId);
            }
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateActivityAsync(Guid sessionId, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.LastActivityAt = DateTimeOffset.UtcNow;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task TerminateSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            _sessions.Remove(sessionId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task TerminateAllUserSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var toRemove = _sessions
                .Where(kvp => kvp.Value.UserId == userId)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var sessionId in toRemove)
            {
                _sessions.Remove(sessionId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task CleanupExpiredSessionsAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var toRemove = _sessions
                .Where(kvp => !kvp.Value.IsActive)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var sessionId in toRemove)
            {
                _sessions.Remove(sessionId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
