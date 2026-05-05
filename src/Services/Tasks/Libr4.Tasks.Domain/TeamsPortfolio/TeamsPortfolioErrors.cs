using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Tasks.Domain.TeamsPortfolio;

public static class TeamsPortfolioErrors
{
    public static readonly Error TeamNotFound = Error.NotFound("team.not_found", "Team not found");
    public static readonly Error NotTeamLead = Error.Forbidden("team.not_lead", "You are not the team lead");
    public static readonly Error NotTeamMember = Error.Forbidden("team.not_member", "You are not a team member");
    public static readonly Error MemberNotFound = Error.NotFound("member.not_found", "Team member not found");
    public static readonly Error PortfolioItemNotFound = Error.NotFound("portfolio.not_found", "Portfolio item not found");
    public static readonly Error ReviewNotFound = Error.NotFound("review.not_found", "Review not found");
    public static readonly Error SkillTestNotFound = Error.NotFound("test.not_found", "Skill test not found");
    public static readonly Error TestResultNotFound = Error.NotFound("result.not_found", "Test result not found");
    public static readonly Error VerificationNotFound = Error.NotFound("verification.not_found", "Verification not found");
    public static readonly Error InvitationNotFound = Error.NotFound("invitation.not_found", "Invitation not found");
    public static readonly Error InvitationExpired = Error.Conflict("invitation.expired", "Invitation has expired");
    public static readonly Error TeamInactive = Error.Conflict("team.inactive", "Team is inactive");
    public static readonly Error MaxAttemptsExceeded = Error.Conflict("test.max_attempts", "Maximum test attempts exceeded");
}
