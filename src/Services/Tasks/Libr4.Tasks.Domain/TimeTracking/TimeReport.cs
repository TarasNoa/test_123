using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TimeTracking;

public sealed class TimeReport : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset EndDate { get; private set; }
    public string ReportType { get; private set; } = "";
    public int TotalSessions { get; private set; }
    public float TotalMinutes { get; private set; }
    public decimal TotalEarnings { get; private set; }
    public decimal? AvgHourlyRate { get; private set; }
    public Dictionary<string, object> ProjectBreakdown { get; private set; } = new();
    public Dictionary<string, object> TaskBreakdown { get; private set; } = new();
    public Dictionary<string, object> DailyBreakdown { get; private set; } = new();
    public Dictionary<string, object> HourlyBreakdown { get; private set; } = new();
    public float? AvgActivityLevel { get; private set; }
    public float? AvgValidationScore { get; private set; }
    public int TotalScreenshots { get; private set; }
    public int FlaggedActivities { get; private set; }
    public float? EfficiencyRate { get; private set; }
    public float? IdlePercentage { get; private set; }
    public float? ProductivityScore { get; private set; }
    public string Status { get; private set; } = "generated";
    public DateTimeOffset GeneratedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private TimeReport() { }

    public static TimeReport Create(
        Guid userId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string reportType,
        DateTimeOffset now)
    {
        return new TimeReport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate,
            ReportType = reportType,
            GeneratedAt = now,
            CreatedAt = now
        };
    }

    public float GetTotalHours()
    {
        return TotalMinutes / 60;
    }

    public float GetAverageDailyHours()
    {
        var days = (int)(EndDate - StartDate).TotalDays + 1;
        return GetTotalHours() / Math.Max(1, days);
    }
}

public sealed class TimeTrackingSettings : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public bool ScreenshotEnabled { get; private set; } = true;
    public int ScreenshotInterval { get; private set; } = 600;
    public int ScreenshotQuality { get; private set; } = 85;
    public bool BlurScreenshots { get; private set; }
    public bool ActivityTrackingEnabled { get; private set; } = true;
    public bool MouseTrackingEnabled { get; private set; } = true;
    public bool KeyboardTrackingEnabled { get; private set; } = true;
    public bool AppTrackingEnabled { get; private set; } = true;
    public bool AutoPauseEnabled { get; private set; } = true;
    public int InactivityTimeout { get; private set; } = 300;
    public int AutoPauseMinDuration { get; private set; } = 600;
    public bool AntiCheatEnabled { get; private set; } = true;
    public bool StrictValidation { get; private set; }
    public float AlertThreshold { get; private set; } = 0.7f;
    public bool PrivateMode { get; private set; }
    public List<string> ExcludeApps { get; private set; } = new();
    public int DataRetentionDays { get; private set; } = 90;
    public bool NotificationsEnabled { get; private set; } = true;
    public bool IdleAlertsEnabled { get; private set; } = true;
    public bool ScreenshotAlertsEnabled { get; private set; }
    public bool AutoReportsEnabled { get; private set; } = true;
    public string ReportFrequency { get; private set; } = "weekly";
    public bool IncludeScreenshots { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private TimeTrackingSettings() { }

    public static TimeTrackingSettings Create(Guid userId, DateTimeOffset now)
    {
        return new TimeTrackingSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateScreenshotSettings(bool enabled, int interval, int quality, bool blur, DateTimeOffset now)
    {
        ScreenshotEnabled = enabled;
        ScreenshotInterval = interval;
        ScreenshotQuality = quality;
        BlurScreenshots = blur;
        UpdatedAt = now;
    }

    public void UpdateActivityTracking(bool enabled, bool mouse, bool keyboard, bool app, DateTimeOffset now)
    {
        ActivityTrackingEnabled = enabled;
        MouseTrackingEnabled = mouse;
        KeyboardTrackingEnabled = keyboard;
        AppTrackingEnabled = app;
        UpdatedAt = now;
    }

    public void UpdateAutoPause(bool enabled, int inactivityTimeout, int minDuration, DateTimeOffset now)
    {
        AutoPauseEnabled = enabled;
        InactivityTimeout = inactivityTimeout;
        AutoPauseMinDuration = minDuration;
        UpdatedAt = now;
    }

    public void UpdateAntiCheat(bool enabled, bool strict, float threshold, DateTimeOffset now)
    {
        AntiCheatEnabled = enabled;
        StrictValidation = strict;
        AlertThreshold = threshold;
        UpdatedAt = now;
    }

    public void UpdatePrivacy(bool privateMode, List<string> excludeApps, int retentionDays, DateTimeOffset now)
    {
        PrivateMode = privateMode;
        ExcludeApps = excludeApps ?? new();
        DataRetentionDays = retentionDays;
        UpdatedAt = now;
    }

    public void UpdateNotifications(bool enabled, bool idleAlerts, bool screenshotAlerts, DateTimeOffset now)
    {
        NotificationsEnabled = enabled;
        IdleAlertsEnabled = idleAlerts;
        ScreenshotAlertsEnabled = screenshotAlerts;
        UpdatedAt = now;
    }

    public void UpdateReporting(bool autoReports, string frequency, bool includeScreenshots, DateTimeOffset now)
    {
        AutoReportsEnabled = autoReports;
        ReportFrequency = frequency;
        IncludeScreenshots = includeScreenshots;
        UpdatedAt = now;
    }
}
