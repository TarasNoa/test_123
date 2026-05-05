using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.TimeTracking;

public sealed class TimeSession : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid? TaskId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string? Description { get; private set; }
    public decimal? HourlyRate { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? StoppedAt { get; private set; }
    public DateTimeOffset LastActivityAt { get; private set; }
    public float? DurationMinutes { get; private set; }
    public float TotalMinutes { get; private set; }
    public float IdleMinutes { get; private set; }
    public decimal? TotalEarnings { get; private set; }
    public SessionStatus Status { get; private set; }
    public string? StopReason { get; private set; }
    public Dictionary<string, object> ComputerInfo { get; private set; } = new();
    public string? AntiCheatFingerprint { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Timezone { get; private set; }
    public string? Location { get; private set; }
    public bool ScreenshotEnabled { get; private set; } = true;
    public bool ActivityTrackingEnabled { get; private set; } = true;
    public bool AutoPauseEnabled { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<TimeEntry> _timeEntries = new();
    private readonly List<Screenshot> _screenshots = new();
    private readonly List<ActivityLog> _activityLogs = new();
    private readonly List<AntiCheatAlert> _antiCheatAlerts = new();

    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();
    public IReadOnlyCollection<Screenshot> Screenshots => _screenshots.AsReadOnly();
    public IReadOnlyCollection<ActivityLog> ActivityLogs => _activityLogs.AsReadOnly();
    public IReadOnlyCollection<AntiCheatAlert> AntiCheatAlerts => _antiCheatAlerts.AsReadOnly();

    private TimeSession() { }

    public static TimeSession Create(
        Guid userId,
        Guid? taskId,
        Guid? projectId,
        string? description,
        decimal? hourlyRate,
        string? timezone,
        string? location,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset now)
    {
        return new TimeSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TaskId = taskId,
            ProjectId = projectId,
            Description = description?.Trim(),
            HourlyRate = hourlyRate,
            StartedAt = now,
            LastActivityAt = now,
            Timezone = timezone,
            Location = location,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Status = SessionStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Stop(string? reason, DateTimeOffset now)
    {
        StoppedAt = now;
        Status = SessionStatus.Completed;
        StopReason = reason;
        DurationMinutes = (float)(now - StartedAt).TotalMinutes;
        UpdatedAt = now;
    }

    public void Pause(DateTimeOffset now)
    {
        Status = SessionStatus.Paused;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void Resume(DateTimeOffset now)
    {
        Status = SessionStatus.Active;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void Flag(DateTimeOffset now)
    {
        Status = SessionStatus.Flagged;
        UpdatedAt = now;
    }

    public void Abandon(DateTimeOffset now)
    {
        Status = SessionStatus.Abandoned;
        StoppedAt = now;
        UpdatedAt = now;
    }

    public void AddTimeEntry(float durationMinutes, string? description, float? activityLevel, string? workType, DateTimeOffset now)
    {
        var entry = new TimeEntry(Id, durationMinutes, description, activityLevel, workType, now);
        _timeEntries.Add(entry);
        TotalMinutes += durationMinutes;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void AddScreenshot(string imageData, int fileSize, string? imageHash, int? width, int? height, float? activityLevel, DateTimeOffset now)
    {
        var screenshot = new Screenshot(Id, imageData, fileSize, imageHash, width, height, activityLevel, now);
        _screenshots.Add(screenshot);
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void AddActivityLog(string activityType, Dictionary<string, object>? details, float? cpuUsage, float? memoryUsage, DateTimeOffset now)
    {
        var log = new ActivityLog(Id, activityType, details, cpuUsage, memoryUsage, now);
        _activityLogs.Add(log);
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void AddAntiCheatAlert(Guid userId, string alertType, AlertSeverity severity, string? description, float? confidenceScore, DateTimeOffset now)
    {
        var alert = new AntiCheatAlert(Id, userId, alertType, severity, description, confidenceScore, now);
        _antiCheatAlerts.Add(alert);
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void SetComputerInfo(Dictionary<string, object> info, string? fingerprint, DateTimeOffset now)
    {
        ComputerInfo = info ?? new();
        AntiCheatFingerprint = fingerprint;
        UpdatedAt = now;
    }

    public float GetDurationHours()
    {
        return (DurationMinutes ?? 0) / 60;
    }

    public float GetEfficiencyRate()
    {
        if (TotalMinutes == 0)
            return 1.0f;
        return (TotalMinutes - IdleMinutes) / TotalMinutes;
    }

    public float GetHourlyEarnings()
    {
        if (!TotalEarnings.HasValue || TotalMinutes == 0)
            return 0;
        return (float)TotalEarnings.Value / (TotalMinutes / 60);
    }

    public bool IsActive => Status == SessionStatus.Active;
}

public sealed class TimeEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SessionId { get; private set; }
    public float DurationMinutes { get; private set; }
    public string? Description { get; private set; }
    public float? ActivityLevel { get; private set; }
    public Dictionary<string, object> MouseActivity { get; private set; } = new();
    public Dictionary<string, object> KeyboardActivity { get; private set; } = new();
    public Dictionary<string, object> ApplicationActivity { get; private set; } = new();
    public int? ValidationScore { get; private set; }
    public Dictionary<string, object> ValidationDetails { get; private set; } = new();
    public string? WorkType { get; private set; }
    public string? ProjectPhase { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private TimeEntry() { }

    internal TimeEntry(Guid sessionId, float durationMinutes, string? description, float? activityLevel, string? workType, DateTimeOffset now)
    {
        SessionId = sessionId;
        DurationMinutes = durationMinutes;
        Description = description?.Trim();
        ActivityLevel = activityLevel;
        WorkType = workType;
        CreatedAt = now;
    }

    public void SetValidation(int score, Dictionary<string, object>? details)
    {
        ValidationScore = score;
        ValidationDetails = details ?? new();
    }

    public bool IsValid => (ValidationScore ?? 0) >= 50;
}

public sealed class Screenshot
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SessionId { get; private set; }
    public string ImageData { get; private set; } = "";
    public int FileSize { get; private set; }
    public string? ImageHash { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public string Format { get; private set; } = "png";
    public int? Quality { get; private set; }
    public float? ActivityLevel { get; private set; }
    public List<string> ActiveApps { get; private set; } = new();
    public string? WindowTitle { get; private set; }
    public Dictionary<string, object> AnalysisResult { get; private set; } = new();
    public float? BlurrinessScore { get; private set; }
    public List<string> SuspiciousElements { get; private set; } = new();
    public ScreenshotStatus Status { get; private set; } = ScreenshotStatus.Scheduled;
    public string? FlaggedReason { get; private set; }
    public DateTimeOffset? ScheduledAt { get; private set; }
    public DateTimeOffset? CapturedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Screenshot() { }

    internal Screenshot(Guid sessionId, string imageData, int fileSize, string? imageHash, int? width, int? height, float? activityLevel, DateTimeOffset now)
    {
        SessionId = sessionId;
        ImageData = imageData;
        FileSize = fileSize;
        ImageHash = imageHash;
        Width = width;
        Height = height;
        ActivityLevel = activityLevel;
        CreatedAt = now;
        ScheduledAt = now;
    }

    public void MarkAsCaptured(DateTimeOffset now)
    {
        Status = ScreenshotStatus.Captured;
        CapturedAt = now;
    }

    public void MarkAsFailed()
    {
        Status = ScreenshotStatus.Failed;
    }

    public void Flag(string reason)
    {
        Status = ScreenshotStatus.Flagged;
        FlaggedReason = reason;
    }

    public bool IsSuspicious => Status == ScreenshotStatus.Flagged || SuspiciousElements.Count > 0;

    public float GetFileSizeMb => FileSize / (1024f * 1024f);
}

public sealed class ActivityLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SessionId { get; private set; }
    public string ActivityType { get; private set; } = "";
    public DateTimeOffset Timestamp { get; private set; }
    public Dictionary<string, object> Details { get; private set; } = new();
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public float? CpuUsage { get; private set; }
    public float? MemoryUsage { get; private set; }
    public Dictionary<string, object> NetworkActivity { get; private set; } = new();
    public Dictionary<string, object> MousePosition { get; private set; } = new();
    public Dictionary<string, object> KeyboardState { get; private set; } = new();
    public string? WindowFocus { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ActivityLog() { }

    internal ActivityLog(Guid sessionId, string activityType, Dictionary<string, object>? details, float? cpuUsage, float? memoryUsage, DateTimeOffset now)
    {
        SessionId = sessionId;
        ActivityType = activityType;
        Details = details ?? new();
        CpuUsage = cpuUsage;
        MemoryUsage = memoryUsage;
        Timestamp = now;
        CreatedAt = now;
    }
}

public sealed class AntiCheatAlert
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public string AlertType { get; private set; } = "";
    public AlertSeverity Severity { get; private set; }
    public string? Description { get; private set; }
    public Dictionary<string, object> Details { get; private set; } = new();
    public Dictionary<string, object> Evidence { get; private set; } = new();
    public float? ConfidenceScore { get; private set; }
    public string Status { get; private set; } = "open";
    public string? Resolution { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public Dictionary<string, object> ActionsTaken { get; private set; } = new();
    public string? PenaltyApplied { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AntiCheatAlert() { }

    internal AntiCheatAlert(Guid sessionId, Guid userId, string alertType, AlertSeverity severity, string? description, float? confidenceScore, DateTimeOffset now)
    {
        SessionId = sessionId;
        UserId = userId;
        AlertType = alertType;
        Severity = severity;
        Description = description;
        ConfidenceScore = confidenceScore;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Resolve(string resolution, Guid resolvedBy, DateTimeOffset now)
    {
        Status = "resolved";
        Resolution = resolution;
        ResolvedBy = resolvedBy;
        ResolvedAt = now;
        UpdatedAt = now;
    }

    public void MarkAsFalsePositive(Guid resolvedBy, DateTimeOffset now)
    {
        Status = "false_positive";
        ResolvedBy = resolvedBy;
        ResolvedAt = now;
        UpdatedAt = now;
    }

    public bool IsHighSeverity => Severity is AlertSeverity.High or AlertSeverity.Critical;
    public bool IsResolved => Status == "resolved";
}

public enum SessionStatus
{
    Active = 0,
    Paused = 1,
    Completed = 2,
    Abandoned = 3,
    Flagged = 4
}

public enum ScreenshotStatus
{
    Scheduled = 0,
    Captured = 1,
    Failed = 2,
    Flagged = 3
}

public enum AlertSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
