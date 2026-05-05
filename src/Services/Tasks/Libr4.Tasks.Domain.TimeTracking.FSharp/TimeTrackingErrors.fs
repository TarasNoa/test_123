namespace Libr4.Tasks.Domain.TimeTracking.FSharp

/// Domain errors for time tracking operations
module TimeTrackingErrors =

    /// Error type for time tracking domain
    type TimeTrackingError =
        | SessionNotFound
        | NotSessionOwner
        | SessionNotActive
        | SessionAlreadyStopped
        | InvalidSessionStatus
        | ScreenshotNotFound
        | ActivityLogNotFound
        | AlertNotFound
        | ReportNotFound
        | SettingsNotFound
        | InvalidDuration
        | InvalidRate
        | InvalidActivityLevel

    /// Convert error to message
    let errorMessage = function
        | SessionNotFound -> "Time session not found"
        | NotSessionOwner -> "You are not the owner of this session"
        | SessionNotActive -> "Session is not active"
        | SessionAlreadyStopped -> "Session is already stopped"
        | InvalidSessionStatus -> "Invalid session status transition"
        | ScreenshotNotFound -> "Screenshot not found"
        | ActivityLogNotFound -> "Activity log not found"
        | AlertNotFound -> "Anti-cheat alert not found"
        | ReportNotFound -> "Time report not found"
        | SettingsNotFound -> "Time tracking settings not found"
        | InvalidDuration -> "Invalid session duration"
        | InvalidRate -> "Invalid hourly rate"
        | InvalidActivityLevel -> "Invalid activity level"

    /// Validation result type
    type ValidationResult<'T> = Result<'T, TimeTrackingError>

    /// Validate session ownership
    let validateSessionOwner (userId: System.Guid) (session: TimeSessionRecord) : ValidationResult<unit> =
        if session.userId = userId then Ok ()
        else Error NotSessionOwner

    /// Validate session is active
    let validateSessionActive (session: TimeSessionRecord) : ValidationResult<unit> =
        match session.status with
        | SessionStatus.Active -> Ok ()
        | _ -> Error SessionNotActive

    /// Validate session not stopped
    let validateSessionNotStopped (session: TimeSessionRecord) : ValidationResult<unit> =
        match session.status with
        | SessionStatus.Completed | SessionStatus.Abandoned -> Error SessionAlreadyStopped
        | _ -> Ok ()

    /// Validate duration
    let validateDuration (minutes: float) : ValidationResult<float> =
        if minutes > 0.0 && minutes <= 1440.0 then Ok minutes
        else Error InvalidDuration

    /// Validate hourly rate
    let validateRate (rate: decimal option) : ValidationResult<decimal option> =
        match rate with
        | None -> Ok None
        | Some r when r > 0m && r <= 10000m -> Ok (Some r)
        | _ -> Error InvalidRate

    /// Validate activity level
    let validateActivityLevel (level: float option) : ValidationResult<float option> =
        match level with
        | None -> Ok None
        | Some l when l >= 0.0 && l <= 100.0 -> Ok (Some l)
        | _ -> Error InvalidActivityLevel
