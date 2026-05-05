using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Tasks.Domain.TimeTracking;

public static class TimeTrackingErrors
{
    public static readonly Error SessionNotFound = Error.NotFound("session.not_found", "Time session not found");
    public static readonly Error NotSessionOwner = Error.Forbidden("session.not_owner", "You are not the owner of this session");
    public static readonly Error SessionNotActive = Error.Conflict("session.not_active", "Session is not active");
    public static readonly Error SessionAlreadyStopped = Error.Conflict("session.already_stopped", "Session is already stopped");
    public static readonly Error InvalidSessionStatus = Error.Conflict("session.invalid_status", "Invalid session status transition");
    public static readonly Error ScreenshotNotFound = Error.NotFound("screenshot.not_found", "Screenshot not found");
    public static readonly Error ActivityLogNotFound = Error.NotFound("log.not_found", "Activity log not found");
    public static readonly Error AlertNotFound = Error.NotFound("alert.not_found", "Anti-cheat alert not found");
    public static readonly Error ReportNotFound = Error.NotFound("report.not_found", "Time report not found");
    public static readonly Error SettingsNotFound = Error.NotFound("settings.not_found", "Time tracking settings not found");
}
