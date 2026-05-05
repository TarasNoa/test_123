using Libr4.Shared.Kernel.Domain;
using Libr4.Auth.Domain.ApiKeys.Events;

namespace Libr4.Auth.Domain.ApiKeys;

public sealed class ApiKey : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = "";
    public string KeyHash { get; private set; } = ""; // SHA-256 of secret
    public string KeyPrefix { get; private set; } = ""; // First 8 chars for display
    public ApiKeyScope Scopes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }

    private ApiKey() { }

    public static ApiKey Issue(Guid userId, string name, string keyHash, string keyPrefix,
        ApiKeyScope scopes, DateTimeOffset now, TimeSpan? lifetime = null)
    {
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Scopes = scopes,
            CreatedAt = now,
            ExpiresAt = lifetime.HasValue ? now.Add(lifetime.Value) : null
        };
        
        apiKey.RaiseDomainEvent(new ApiKeyIssuedEvent(apiKey.Id, userId, name, scopes, now));
        return apiKey;
    }

    public bool IsActive(DateTimeOffset now)
        => RevokedAt is null && (ExpiresAt is null || ExpiresAt.Value > now);

    public void RecordUsage(DateTimeOffset now)
    {
        LastUsedAt = now;
        RaiseDomainEvent(new ApiKeyUsedEvent(Id, UserId, now));
    }

    public void Revoke(string? reason, DateTimeOffset now)
    {
        if (RevokedAt.HasValue) return;
        RevokedAt = now;
        RevokedReason = reason;
        RaiseDomainEvent(new ApiKeyRevokedEvent(Id, UserId, reason, now));
    }
}

[Flags]
public enum ApiKeyScope
{
    None = 0,
    ReadProfile = 1 << 0,
    WriteProfile = 1 << 1,
    ReadTasks = 1 << 2,
    WriteTasks = 1 << 3,
    ReadPayments = 1 << 4,
    WritePayments = 1 << 5,
    ReadChat = 1 << 6,
    WriteChat = 1 << 7,
    Admin = 1 << 30,
    All = ReadProfile | WriteProfile | ReadTasks | WriteTasks | ReadPayments | WritePayments | ReadChat | WriteChat
}
