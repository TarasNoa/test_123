using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Tasks.Domain.Projects;

public static class ProjectErrors
{
    public static readonly Error ProjectNotFound = Error.NotFound("projects.not_found", "Project not found");
    public static readonly Error NotProjectOwner = Error.Forbidden("projects.not_owner", "You are not the owner of this project");
    public static readonly Error MemberNotFound = Error.NotFound("projects.member_not_found", "Team member not found");
    public static readonly Error TaskNotFound = Error.NotFound("projects.task_not_found", "Project task not found");
    public static readonly Error MilestoneNotFound = Error.NotFound("projects.milestone_not_found", "Milestone not found");
    public static readonly Error TeamAtCapacity = Error.Conflict("projects.team_at_capacity", "Project team is at maximum capacity");
    public static readonly Error AlreadyMember = Error.Conflict("projects.already_member", "User is already a member of this project");
    public static readonly Error InvalidProgress = Error.Validation("projects.invalid_progress", "Progress must be between 0 and 100");
    public static readonly Error InvalidStatus = Error.Validation("projects.invalid_status", "Invalid project status");
}
