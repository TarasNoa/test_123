using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Tasks.Domain.Interactions;

public static class InteractionErrors
{
    public static readonly Error LikeNotFound = Error.NotFound("interactions.like_not_found", "Like not found");
    public static readonly Error BookmarkNotFound = Error.NotFound("interactions.bookmark_not_found", "Bookmark not found");
    public static readonly Error FollowNotFound = Error.NotFound("interactions.follow_not_found", "Follow not found");
    public static readonly Error AlreadyLiked = Error.Conflict("interactions.already_liked", "You have already liked this item");
    public static readonly Error AlreadyBookmarked = Error.Conflict("interactions.already_bookmarked", "You have already bookmarked this item");
    public static readonly Error AlreadyFollowing = Error.Conflict("interactions.already_following", "You are already following this user");
    public static readonly Error CannotFollowYourself = Error.Validation("interactions.cannot_follow_self", "Cannot follow yourself");
}
