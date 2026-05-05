using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Tasks.Domain.DisputeResolution;

public static class DisputeErrors
{
    public static readonly Error DisputeNotFound = Error.NotFound("dispute.not_found", "Dispute not found");
    public static readonly Error NotInitiator = Error.Forbidden("dispute.not_initiator", "You are not the initiator of this dispute");
    public static readonly Error NotRespondent = Error.Forbidden("dispute.not_respondent", "You are not the respondent of this dispute");
    public static readonly Error NotModerator = Error.Forbidden("dispute.not_moderator", "You are not the assigned moderator");
    public static readonly Error NotArbitrator = Error.Forbidden("dispute.not_arbitrator", "You are not the assigned arbitrator");
    public static readonly Error InvalidStatusTransition = Error.Conflict("dispute.invalid_status", "Invalid dispute status transition");
    public static readonly Error DisputeAlreadyResolved = Error.Conflict("dispute.already_resolved", "Dispute is already resolved");
    public static readonly Error EvidenceNotFound = Error.NotFound("evidence.not_found", "Evidence not found");
    public static readonly Error ResolutionNotFound = Error.NotFound("resolution.not_found", "Resolution not found");
    public static readonly Error ResolutionExpired = Error.Conflict("resolution.expired", "Resolution proposal has expired");
    public static readonly Error MessageNotFound = Error.NotFound("message.not_found", "Message not found");
}
