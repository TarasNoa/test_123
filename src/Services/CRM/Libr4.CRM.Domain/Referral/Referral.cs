using Libr4.Shared.Kernel.Domain;

namespace Libr4.CRM.Domain.Referral;

public enum ReferralStatus
{
    Pending,
    Active,
    Completed,
    Cancelled,
    Expired
}

public class ReferralCode : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int TotalReferrals { get; private set; }
    public decimal TotalEarnings { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private readonly List<Referral> _referrals = new();
    public IReadOnlyCollection<Referral> Referrals => _referrals.AsReadOnly();

    private ReferralCode() { }

    public static ReferralCode Create(Guid userId, string code, DateTimeOffset? expiresAt = null)
    {
        return new ReferralCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = code,
            IsActive = true,
            TotalReferrals = 0,
            TotalEarnings = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public void AddReferral(Referral referral)
    {
        _referrals.Add(referral);
        TotalReferrals++;
    }

    public void RecordEarnings(decimal amount)
    {
        TotalEarnings += amount;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt < DateTimeOffset.UtcNow;
}

public class Referral : Entity<Guid>
{
    public Guid ReferrerId { get; private set; }
    public Guid? ReferredUserId { get; private set; }
    public string ReferralCode { get; private set; } = string.Empty;
    public ReferralStatus Status { get; private set; }
    public decimal RewardAmount { get; private set; }
    public bool RewardPaid { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private Referral() { }

    public static Referral Create(Guid referrerId, string referralCode, decimal rewardAmount)
    {
        return new Referral
        {
            Id = Guid.NewGuid(),
            ReferrerId = referrerId,
            ReferralCode = referralCode,
            Status = ReferralStatus.Pending,
            RewardAmount = rewardAmount,
            RewardPaid = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Complete(Guid referredUserId)
    {
        Status = ReferralStatus.Completed;
        ReferredUserId = referredUserId;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        Status = ReferralStatus.Active;
    }

    public void Cancel()
    {
        Status = ReferralStatus.Cancelled;
    }

    public void Expire()
    {
        Status = ReferralStatus.Expired;
    }

    public void MarkRewardPaid()
    {
        RewardPaid = true;
    }
}

public class ReferralSettings : Entity<Guid>
{
    public decimal DefaultRewardAmount { get; private set; }
    public int ReferralBonusPercentage { get; private set; }
    public int MaxReferralsPerUser { get; private set; }
    public int ReferralValidityDays { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ReferralSettings() { }

    public static ReferralSettings Create(
        decimal defaultRewardAmount,
        int referralBonusPercentage,
        int maxReferralsPerUser,
        int referralValidityDays)
    {
        return new ReferralSettings
        {
            Id = Guid.NewGuid(),
            DefaultRewardAmount = defaultRewardAmount,
            ReferralBonusPercentage = referralBonusPercentage,
            MaxReferralsPerUser = maxReferralsPerUser,
            ReferralValidityDays = referralValidityDays,
            IsActive = true,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateRewardAmount(decimal amount)
    {
        DefaultRewardAmount = amount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateBonusPercentage(int percentage)
    {
        ReferralBonusPercentage = percentage;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateMaxReferrals(int max)
    {
        MaxReferralsPerUser = max;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateValidityDays(int days)
    {
        ReferralValidityDays = days;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
