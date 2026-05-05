using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Tasks.Domain.BlindApplications;

public static class BlindApplicationErrors
{
    public static readonly Error ApplicationNotFound = Error.NotFound("blind_app.not_found", "Blind application not found");
    public static readonly Error NotApplicant = Error.Forbidden("blind_app.not_applicant", "You are not the applicant for this application");
    public static readonly Error AlreadyApplied = Error.Conflict("blind_app.already_applied", "You have already applied to this task");
    public static readonly Error TaskNotAcceptingApplications = Error.Conflict("blind_app.task_closed", "Task is not accepting applications");
    public static readonly Error AlreadyRevealed = Error.Conflict("blind_app.already_revealed", "Application has already been revealed");
    public static readonly Error InvalidStatus = Error.Validation("blind_app.invalid_status", "Invalid application status");
}
