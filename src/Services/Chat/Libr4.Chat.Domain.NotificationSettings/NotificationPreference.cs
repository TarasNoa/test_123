using System;
using System.Collections.Generic;

namespace Libr4.Chat.Domain.NotificationSettings;

public enum NotificationChannel { Email, Push, SMS, Telegram, InApp }
public enum NotificationFrequency { Instant, Hourly, Daily, Weekly, Never }

public class NotificationPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationFrequency Frequency { get; set; } = NotificationFrequency.Instant;
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }
}

public class QuietHours
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<DayOfWeek> AppliedDays { get; set; } = [];
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsInQuietHours(DateTime now)
    {
        if (!IsEnabled || !AppliedDays.Contains(now.DayOfWeek)) return false;
        var currentTime = now.TimeOfDay;
        return currentTime >= StartTime && currentTime <= EndTime;
    }
}

public class NotificationCategory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<NotificationChannel, NotificationFrequency> ChannelPreferences { get; set; } = [];
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }
}
