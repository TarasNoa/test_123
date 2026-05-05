using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Tasks.Domain.Portfolio;

public static class PortfolioErrors
{
    public static readonly Error ItemNotFound = Error.NotFound("portfolio.item_not_found", "Portfolio item not found");
    public static readonly Error NotItemOwner = Error.Forbidden("portfolio.not_owner", "You are not the owner of this portfolio item");
    public static readonly Error AlreadyPublished = Error.Conflict("portfolio.already_published", "Portfolio item is already published");
    public static readonly Error InvalidStatus = Error.Validation("portfolio.invalid_status", "Invalid portfolio item status");
}
