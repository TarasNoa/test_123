namespace Libr4.Tasks.Domain.TimeTracking.FSharp

open System

/// TimeSession factory and operations module
module TimeSessionOps =

    /// Create a new time session
    let create 
        (userId: Guid)
        (taskId: Guid option)
        (projectId: Guid option)
        (description: string option)
        (hourlyRate: decimal option)
        (timezone: string option)
        (location: string option)
        (ipAddress: string option)
        (userAgent: string option)
        (now: DateTimeOffset) : TimeSessionRecord =
        {
            id = Guid.NewGuid()
            userId = userId
            taskId = taskId
            projectId = projectId
            description = description |> Option.map (fun s -> s.Trim())
            hourlyRate = hourlyRate
            startedAt = now
            stoppedAt = None
            lastActivityAt = now
            durationMinutes = None
            totalMinutes = 0.0
            idleMinutes = 0.0
            totalEarnings = None
            status = SessionStatus.Active
            stopReason = None
            computerInfo = Map.empty
            antiCheatFingerprint = None
            ipAddress = ipAddress
            userAgent = userAgent
            timezone = timezone
            location = location
            screenshotEnabled = true
            activityTrackingEnabled = true
            autoPauseEnabled = true
            timeEntries = []
            screenshots = []
            activityLogs = []
            antiCheatAlerts = []
            createdAt = now
            updatedAt = now
        }

    /// Stop the session
    let stop (reason: string option) (now: DateTimeOffset) (session: TimeSessionRecord) : TimeSessionRecord =
        { session with
            stoppedAt = Some now
            status = SessionStatus.Completed
            stopReason = reason
            durationMinutes = Some (float (now - session.startedAt).TotalMinutes)
            updatedAt = now
        }

    /// Pause the session
    let pause (now: DateTimeOffset) (session: TimeSessionRecord) : TimeSessionRecord =
        { session with
            status = SessionStatus.Paused
            lastActivityAt = now
            updatedAt = now
        }

    /// Resume the session
    let resume (now: DateTimeOffset) (session: TimeSessionRecord) : TimeSessionRecord =
        { session with
            status = SessionStatus.Active
            lastActivityAt = now
            updatedAt = now
        }

    /// Flag the session
    let flag (now: DateTimeOffset) (session: TimeSessionRecord) : TimeSessionRecord =
        { session with
            status = SessionStatus.Flagged
            updatedAt = now
        }

    /// Abandon the session
    let abandon (now: DateTimeOffset) (session: TimeSessionRecord) : TimeSessionRecord =
        { session with
            status = SessionStatus.Abandoned
            stoppedAt = Some now
            updatedAt = now
        }

    /// Add a time entry to the session
    let addTimeEntry 
        (durationMinutes: float)
        (description: string option)
        (activityLevel: float option)
        (workType: string option)
        (now: DateTimeOffset)
        (session: TimeSessionRecord) : TimeSessionRecord =
        let entry: TimeEntry = {
            id = Guid.NewGuid()
            sessionId = session.id
            durationMinutes = durationMinutes
            description = description |> Option.map (fun s -> s.Trim())
            activityLevel = activityLevel
            mouseActivity = Map.empty
            keyboardActivity = Map.empty
            applicationActivity = Map.empty
            validationScore = None
            validationDetails = Map.empty
            workType = workType
            projectPhase = None
            createdAt = now
        }
        { session with
            timeEntries = session.timeEntries @ [entry]
            totalMinutes = session.totalMinutes + durationMinutes
            lastActivityAt = now
            updatedAt = now
        }

    /// Add a screenshot to the session
    let addScreenshot
        (imageData: string)
        (fileSize: int)
        (imageHash: string option)
        (width: int option)
        (height: int option)
        (activityLevel: float option)
        (now: DateTimeOffset)
        (session: TimeSessionRecord) : TimeSessionRecord =
        let screenshot: Screenshot = {
            id = Guid.NewGuid()
            sessionId = session.id
            imageData = imageData
            fileSize = fileSize
            imageHash = imageHash
            width = width
            height = height
            format = "png"
            quality = None
            activityLevel = activityLevel
            activeApps = []
            windowTitle = None
            analysisResult = Map.empty
            blurrinessScore = None
            suspiciousElements = []
            status = ScreenshotStatus.Scheduled
            flaggedReason = None
            scheduledAt = Some now
            capturedAt = None
            createdAt = now
        }
        { session with
            screenshots = session.screenshots @ [screenshot]
            lastActivityAt = now
            updatedAt = now
        }

    /// Add an activity log to the session
    let addActivityLog
        (activityType: string)
        (details: Map<string, obj> option)
        (cpuUsage: float option)
        (memoryUsage: float option)
        (now: DateTimeOffset)
        (session: TimeSessionRecord) : TimeSessionRecord =
        let log: ActivityLog = {
            id = Guid.NewGuid()
            sessionId = session.id
            activityType = activityType
            timestamp = now
            details = details |> Option.defaultValue Map.empty
            metadata = Map.empty
            cpuUsage = cpuUsage
            memoryUsage = memoryUsage
            networkActivity = Map.empty
            mousePosition = Map.empty
            keyboardState = Map.empty
            windowFocus = None
            createdAt = now
        }
        { session with
            activityLogs = session.activityLogs @ [log]
            lastActivityAt = now
            updatedAt = now
        }

    /// Add an anti-cheat alert to the session
    let addAntiCheatAlert
        (userId: Guid)
        (alertType: string)
        (severity: AlertSeverity)
        (description: string option)
        (confidenceScore: float option)
        (now: DateTimeOffset)
        (session: TimeSessionRecord) : TimeSessionRecord =
        let alert: AntiCheatAlert = {
            id = Guid.NewGuid()
            sessionId = session.id
            userId = userId
            alertType = alertType
            severity = severity
            description = description
            details = Map.empty
            evidence = Map.empty
            confidenceScore = confidenceScore
            status = "open"
            resolution = None
            resolvedAt = None
            resolvedBy = None
            actionsTaken = Map.empty
            penaltyApplied = None
            createdAt = now
            updatedAt = now
        }
        { session with
            antiCheatAlerts = session.antiCheatAlerts @ [alert]
            lastActivityAt = now
            updatedAt = now
        }

    /// Set computer info
    let setComputerInfo
        (info: Map<string, obj>)
        (fingerprint: string option)
        (now: DateTimeOffset)
        (session: TimeSessionRecord) : TimeSessionRecord =
        { session with
            computerInfo = info
            antiCheatFingerprint = fingerprint
            updatedAt = now
        }

    /// Get duration in hours
    let getDurationHours (session: TimeSessionRecord) : float =
        (session.durationMinutes |> Option.defaultValue 0.0) / 60.0

    /// Get efficiency rate (active time vs total time)
    let getEfficiencyRate (session: TimeSessionRecord) : float =
        if session.totalMinutes = 0.0 then 1.0
        else (session.totalMinutes - session.idleMinutes) / session.totalMinutes

    /// Get hourly earnings
    let getHourlyEarnings (session: TimeSessionRecord) : float =
        match session.totalEarnings, session.totalMinutes with
        | Some earnings, minutes when minutes > 0.0 -> float earnings / (minutes / 60.0)
        | _ -> 0.0

    /// Check if session is active
    let isActive (session: TimeSessionRecord) : bool =
        session.status = SessionStatus.Active

/// TimeReport factory and operations module
module TimeReportOps =

    /// Create a new time report
    let create
        (userId: Guid)
        (startDate: DateTimeOffset)
        (endDate: DateTimeOffset)
        (reportType: string)
        (now: DateTimeOffset) : TimeReportRecord =
        {
            id = Guid.NewGuid()
            userId = userId
            startDate = startDate
            endDate = endDate
            reportType = reportType
            totalSessions = 0
            totalMinutes = 0.0
            totalEarnings = 0m
            avgHourlyRate = None
            projectBreakdown = Map.empty
            taskBreakdown = Map.empty
            dailyBreakdown = Map.empty
            hourlyBreakdown = Map.empty
            avgActivityLevel = None
            avgValidationScore = None
            totalScreenshots = 0
            flaggedActivities = 0
            efficiencyRate = None
            idlePercentage = None
            productivityScore = None
            status = "generated"
            generatedAt = now
            createdAt = now
        }

    /// Get total hours
    let getTotalHours (report: TimeReportRecord) : float =
        report.totalMinutes / 60.0

    /// Get average daily hours
    let getAverageDailyHours (report: TimeReportRecord) : float =
        let days = (report.endDate - report.startDate).Days + 1
        (getTotalHours report) / float (max 1 days)

/// TimeTrackingSettings factory and operations module
module TimeTrackingSettingsOps =

    /// Create default settings for a user
    let create (userId: Guid) (now: DateTimeOffset) : TimeTrackingSettingsRecord =
        {
            id = Guid.NewGuid()
            userId = userId
            screenshotEnabled = true
            screenshotInterval = 600
            screenshotQuality = 85
            blurScreenshots = false
            activityTrackingEnabled = true
            mouseTrackingEnabled = true
            keyboardTrackingEnabled = true
            appTrackingEnabled = true
            autoPauseEnabled = true
            inactivityTimeout = 300
            autoPauseMinDuration = 600
            antiCheatEnabled = true
            strictValidation = false
            alertThreshold = 0.7
            privateMode = false
            excludeApps = []
            dataRetentionDays = 90
            notificationsEnabled = true
            idleAlertsEnabled = true
            screenshotAlertsEnabled = false
            autoReportsEnabled = true
            reportFrequency = "weekly"
            includeScreenshots = false
            createdAt = now
            updatedAt = now
        }

    /// Update screenshot settings
    let updateScreenshotSettings
        (enabled: bool)
        (interval: int)
        (quality: int)
        (blur: bool)
        (now: DateTimeOffset)
        (settings: TimeTrackingSettingsRecord) : TimeTrackingSettingsRecord =
        { settings with
            screenshotEnabled = enabled
            screenshotInterval = interval
            screenshotQuality = quality
            blurScreenshots = blur
            updatedAt = now
        }

    /// Update activity tracking settings
    let updateActivityTracking
        (enabled: bool)
        (mouse: bool)
        (keyboard: bool)
        (app: bool)
        (now: DateTimeOffset)
        (settings: TimeTrackingSettingsRecord) : TimeTrackingSettingsRecord =
        { settings with
            activityTrackingEnabled = enabled
            mouseTrackingEnabled = mouse
            keyboardTrackingEnabled = keyboard
            appTrackingEnabled = app
            updatedAt = now
        }

    /// Update anti-cheat settings
    let updateAntiCheat
        (enabled: bool)
        (strict: bool)
        (threshold: float)
        (now: DateTimeOffset)
        (settings: TimeTrackingSettingsRecord) : TimeTrackingSettingsRecord =
        { settings with
            antiCheatEnabled = enabled
            strictValidation = strict
            alertThreshold = threshold
            updatedAt = now
        }
