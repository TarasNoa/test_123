using System;

namespace Libr4.Payments.Domain.Monetization;

public enum CommissionType { Percentage, Fixed, Tiered }

public class CommissionRule
{
    public Guid Id { get; set; }
    public CommissionType Type { get; set; }
    public decimal Rate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public decimal CalculateCommission(decimal amount) =>
        Type switch
        {
            CommissionType.Percentage => amount * Rate / 100,
            CommissionType.Fixed => Rate,
            _ => 0
        };
}

public class RevenueShare
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty; // "referral", "affiliate", "partner"
    public decimal Percentage { get; set; }
    public decimal TotalEarned { get; set; }
    public decimal Paid { get; set; }
    public decimal Pending => TotalEarned - Paid;
    public DateTimeOffset CreatedAt { get; set; }

    public void AddEarnings(decimal amount) => TotalEarned += amount;
    public void MarkAsPaid(decimal amount) => Paid += amount;
}

public class PlatformFeeRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset Period { get; set; }
    public decimal TotalTransactionVolume { get; set; }
    public decimal CommissionCollected { get; set; }
    public decimal FeeAmount => CommissionCollected * 0.1m; // 10% of commissions
    public decimal NetRevenue => CommissionCollected - FeeAmount;
}
