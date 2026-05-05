using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Tasks.Domain.WorkDelivery;

public static class WorkDeliveryErrors
{
    public static readonly Error DeliveryNotFound = Error.NotFound("delivery.not_found", "Work delivery not found");
    public static readonly Error NotFreelancer = Error.Forbidden("delivery.not_freelancer", "You are not the freelancer for this delivery");
    public static readonly Error NotClient = Error.Forbidden("delivery.not_client", "You are not the client for this delivery");
    public static readonly Error InvalidStatus = Error.Conflict("delivery.invalid_status", "Invalid delivery status transition");
    public static readonly Error PreviewAlreadyActive = Error.Conflict("delivery.preview_active", "Preview is already active");
    public static readonly Error PreviewNotActive = Error.Conflict("delivery.preview_not_active", "Preview is not currently active");
    public static readonly Error SessionNotFound = Error.NotFound("session.not_found", "Preview session not found");
    public static readonly Error SessionExpired = Error.Conflict("session.expired", "Preview session has expired");
    public static readonly Error FileTooLarge = Error.Validation("delivery.file_too_large", "File exceeds maximum size (50MB)");
}
