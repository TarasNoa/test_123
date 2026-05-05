using System;
using System.Collections.Generic;

namespace Libr4.Trading.Domain.TradingViewIntegration;

public enum AlertStatus { Active, Triggered, Disabled, Expired }
public enum AlertCondition { PriceAbove, PriceBelow, CrossAbove, CrossBelow, IndicatorAbove, IndicatorBelow }

public class Chart
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Interval { get; set; } = "1h";
    public string ChartType { get; set; } = "candle"; // candle, line, bar, hollow
    public List<string> Indicators { get; set; } = new List<string>();
    public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    public string? SavedLayout { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class Alert
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AlertCondition Condition { get; set; }
    public decimal TargetValue { get; set; }
    public string? IndicatorName { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Active;
    public int TriggerCount { get; set; }
    public DateTimeOffset? LastTriggeredAt { get; set; }
    public string? NotificationChannel { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public bool IsTriggered => Status == AlertStatus.Triggered;
    public void Trigger(DateTimeOffset now) { Status = AlertStatus.Triggered; LastTriggeredAt = now; TriggerCount++; }
}

public class WebhookSignal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty; // buy, sell, alert
    public Dictionary<string, object> Payload { get; set; } = new Dictionary<string, object>();
    public bool WasExecuted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
