using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Tasks.Domain;

public static class TasksErrors
{
    public static readonly Error NotFound = Error.NotFound("tasks.not_found", "Not found");
    public static readonly Error TaskNotFound = Error.NotFound("tasks.not_found", "Task not found");
    public static readonly Error ApplicationNotFound = Error.NotFound("tasks.application_not_found", "Application not found");
    public static readonly Error ReviewNotFound = Error.NotFound("tasks.review_not_found", "Review not found");
    public static readonly Error NotTaskOwner = Error.Forbidden("tasks.not_owner", "You are not the owner of this task");
    public static readonly Error NotApplicationOwner = Error.Forbidden("tasks.not_application_owner", "You are not the owner of this application");
    public static readonly Error InvalidStatusTransition = Error.Validation("tasks.invalid_status", "Invalid status transition");
    public static readonly Error AlreadyApplied = Error.Conflict("tasks.already_applied", "You have already applied to this task");
    public static readonly Error TaskNotOpen = Error.Conflict("tasks.not_open", "Task is not open for applications");
    public static readonly Error InvalidRating = Error.Validation("tasks.invalid_rating", "Rating must be between 1 and 5");
    public static readonly Error ReviewAlreadyExists = Error.Conflict("tasks.review_exists", "You have already reviewed this task");
}
