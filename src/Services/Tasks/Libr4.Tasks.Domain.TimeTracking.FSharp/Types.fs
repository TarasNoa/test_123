namespace Libr4.Tasks.Domain.TimeTracking.FSharp

open System

/// Session status discriminated union
type SessionStatus =
    | Active
    | Paused
    | Completed
    | Abandoned
    | Flagged

/// Screenshot status discriminated union
type ScreenshotStatus =
    | Scheduled
    | Captured
    | Failed
    | Flagged

/// Alert severity levels
type AlertSeverity =
    | Low
    | Medium
    | High
    | Critical

/// Time entry record
type TimeEntry = {
    id: Guid
    sessionId: Guid
    durationMinutes: float
    description: string option
    activityLevel: float option
    mouseActivity: Map<string, obj>
    keyboardActivity: Map<string, obj>
    applicationActivity: Map<string, obj>
    validationScore: int option
    validationDetails: Map<string, obj>
    workType: string option
    projectPhase: string option
    createdAt: DateTimeOffset
}

/// Screenshot record
type Screenshot = {
    id: Guid
    sessionId: Guid
    imageData: string
    fileSize: int
    imageHash: string option
    width: int option
    height: int option
    format: string
    quality: int option
    activityLevel: float option
    activeApps: string list
    windowTitle: string option
    analysisResult: Map<string, obj>
    blurrinessScore: float option
    suspiciousElements: string list
    status: ScreenshotStatus
    flaggedReason: string option
    scheduledAt: DateTimeOffset option
    capturedAt: DateTimeOffset option
    createdAt: DateTimeOffset
}

/// Activity log record
type ActivityLog = {
    id: Guid
    sessionId: Guid
    activityType: string
    timestamp: DateTimeOffset
    details: Map<string, obj>
    metadata: Map<string, obj>
    cpuUsage: float option
    memoryUsage: float option
    networkActivity: Map<string, obj>
    mousePosition: Map<string, obj>
    keyboardState: Map<string, obj>
    windowFocus: string option
    createdAt: DateTimeOffset
}

/// Anti-cheat alert record
type AntiCheatAlert = {
    id: Guid
    sessionId: Guid
    userId: Guid
    alertType: string
    severity: AlertSeverity
    description: string option
    details: Map<string, obj>
    evidence: Map<string, obj>
    confidenceScore: float option
    status: string
    resolution: string option
    resolvedAt: DateTimeOffset option
    resolvedBy: Guid option
    actionsTaken: Map<string, obj>
    penaltyApplied: string option
    createdAt: DateTimeOffset
    updatedAt: DateTimeOffset
}

/// Time session aggregate root
type TimeSessionRecord = {
    id: Guid
    userId: Guid
    taskId: Guid option
    projectId: Guid option
    description: string option
    hourlyRate: decimal option
    startedAt: DateTimeOffset
    stoppedAt: DateTimeOffset option
    lastActivityAt: DateTimeOffset
    durationMinutes: float option
    totalMinutes: float
    idleMinutes: float
    totalEarnings: decimal option
    status: SessionStatus
    stopReason: string option
    computerInfo: Map<string, obj>
    antiCheatFingerprint: string option
    ipAddress: string option
    userAgent: string option
    timezone: string option
    location: string option
    screenshotEnabled: bool
    activityTrackingEnabled: bool
    autoPauseEnabled: bool
    timeEntries: TimeEntry list
    screenshots: Screenshot list
    activityLogs: ActivityLog list
    antiCheatAlerts: AntiCheatAlert list
    createdAt: DateTimeOffset
    updatedAt: DateTimeOffset
}

/// Time report record
type TimeReportRecord = {
    id: Guid
    userId: Guid
    startDate: DateTimeOffset
    endDate: DateTimeOffset
    reportType: string
    totalSessions: int
    totalMinutes: float
    totalEarnings: decimal
    avgHourlyRate: decimal option
    projectBreakdown: Map<string, obj>
    taskBreakdown: Map<string, obj>
    dailyBreakdown: Map<string, obj>
    hourlyBreakdown: Map<string, obj>
    avgActivityLevel: float option
    avgValidationScore: float option
    totalScreenshots: int
    flaggedActivities: int
    efficiencyRate: float option
    idlePercentage: float option
    productivityScore: float option
    status: string
    generatedAt: DateTimeOffset
    createdAt: DateTimeOffset
}

/// Time tracking settings record
type TimeTrackingSettingsRecord = {
    id: Guid
    userId: Guid
    screenshotEnabled: bool
    screenshotInterval: int
    screenshotQuality: int
    blurScreenshots: bool
    activityTrackingEnabled: bool
    mouseTrackingEnabled: bool
    keyboardTrackingEnabled: bool
    appTrackingEnabled: bool
    autoPauseEnabled: bool
    inactivityTimeout: int
    autoPauseMinDuration: int
    antiCheatEnabled: bool
    strictValidation: bool
    alertThreshold: float
    privateMode: bool
    excludeApps: string list
    dataRetentionDays: int
    notificationsEnabled: bool
    idleAlertsEnabled: bool
    screenshotAlertsEnabled: bool
    autoReportsEnabled: bool
    reportFrequency: string
    includeScreenshots: bool
    createdAt: DateTimeOffset
    updatedAt: DateTimeOffset
}
