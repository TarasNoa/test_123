using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Security;

/// <summary>
/// Step-up authentication challenge for sensitive operations
/// (e.g. payment confirmation, profile changes, admin actions).
/// </summary>
public sealed class SecurityChallenge : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public SecurityChallengeKind Kind { get; private set; }
    public string CodeHash { get; private set; } = "";
    public string PurposeKey { get; private set; } = ""; // e.g. "payment.confirm:guid"
    public int Attempts { get; private set; }
    public int MaxAttempts { get; private set; } = 3;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }

    private SecurityChallenge() { }

    public static SecurityChallenge Issue(Guid userId, SecurityChallengeKind kind, string codeHash,
        string purposeKey, DateTimeOffset now, TimeSpan? lifetime = null)
    {
        return new SecurityChallenge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = kind,
            CodeHash = codeHash,
            PurposeKey = purposeKey,
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime ?? TimeSpan.FromMinutes(10))
        };
    }

    public bool IsActive(DateTimeOffset now)
        => ConsumedAt is null && FailedAt is null && ExpiresAt > now && Attempts < MaxAttempts;

    public bool Verify(string codeHash, DateTimeOffset now)
    {
        if (!IsActive(now)) return false;
        Attempts++;
        if (CodeHash == codeHash)
        {
            ConsumedAt = now;
            return true;
        }
        if (Attempts >= MaxAttempts) FailedAt = now;
        return false;
    }
}

public enum SecurityChallengeKind { EmailCode = 0, SmsCode = 1, TotpCode = 2, BackupCode = 3, WebauthnAssertion = 4 }
