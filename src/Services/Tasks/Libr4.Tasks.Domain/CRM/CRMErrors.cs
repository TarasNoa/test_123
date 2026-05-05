using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Tasks.Domain.CRM;

public static class CRMErrors
{
    public static readonly Error AccountNotFound = Error.NotFound("crm.account_not_found", "CRM account not found");
    public static readonly Error ContactNotFound = Error.NotFound("crm.contact_not_found", "Contact not found");
    public static readonly Error DealNotFound = Error.NotFound("crm.deal_not_found", "Deal not found");
    public static readonly Error TaskNotFound = Error.NotFound("crm.task_not_found", "CRM task not found");
    public static readonly Error PipelineNotFound = Error.NotFound("crm.pipeline_not_found", "Pipeline not found");
    public static readonly Error NotAccountOwner = Error.Forbidden("crm.not_owner", "You are not the owner of this CRM account");
    public static readonly Error AccountLimitExceeded = Error.Conflict("crm.limit_exceeded", "Account limit exceeded for subscription plan");
    public static readonly Error InvalidDealStage = Error.Validation("crm.invalid_stage", "Invalid deal stage");
}
