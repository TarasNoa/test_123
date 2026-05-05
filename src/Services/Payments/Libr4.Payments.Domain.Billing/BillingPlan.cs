using System;
using System.Collections.Generic;

namespace Libr4.Payments.Domain.Billing;

public enum BillingCycle { Monthly, Quarterly, Yearly, Custom }
public enum SubscriptionStatus { Active, Paused, Cancelled, Expired }

public class BillingPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public BillingCycle Cycle { get; set; }
    public int CycleDays { get; set; }
    public List<string> Features { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}

public class Subscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset? NextBillingDate { get; set; }
    public decimal Amount { get; set; }
    public int RenewalCount { get; set; }
    public bool AutoRenew { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void Cancel(DateTimeOffset now) { Status = SubscriptionStatus.Cancelled; EndDate = now; UpdatedAt = now; }
    public void Pause(DateTimeOffset now) { Status = SubscriptionStatus.Paused; UpdatedAt = now; }
    public void Resume(DateTimeOffset now) { Status = SubscriptionStatus.Active; UpdatedAt = now; }
    public bool IsExpired() => EndDate.HasValue && DateTimeOffset.UtcNow > EndDate.Value;
}
