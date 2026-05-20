using Libr4.Social.Application.Abstractions;
using Libr4.Social.Application.Commands;
using Libr4.Social.Application.Queries;
using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Libr4.Social.Api.Endpoints;

public static class SocialEndpointsV2
{
    public static void MapSocialNetworkEndpointsV2(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/social")
            .WithTags("Social Network V2")
            .RequireAuthorization();

        // Profile endpoints
        group.MapGet("/profile", GetProfile)
            .WithName("GetProfileV2")
            .WithCacheableResponse(TimeSpan.FromMinutes(30));

        group.MapPut("/profile", UpdateProfile)
            .WithName("UpdateProfileV2")
            .InvalidatesCache("profile:*");

        // Connection endpoints
        group.MapPost("/connections", AddConnection)
            .WithName("AddConnectionV2");

        group.MapDelete("/connections/{connectedUserId}", RemoveConnection)
            .WithName("RemoveConnectionV2");

        group.MapGet("/connections", GetConnections)
            .WithName("GetConnectionsV2")
            .WithCacheableResponse(TimeSpan.FromHours(1));

        // Follow endpoints
        group.MapPost("/follow/{targetUserId}", FollowUser)
            .WithName("FollowUserV2");

        group.MapDelete("/follow/{targetUserId}", UnfollowUser)
            .WithName("UnfollowUserV2");

        group.MapGet("/followers", GetFollowers)
            .WithName("GetFollowersV2")
            .WithCacheableResponse(TimeSpan.FromMinutes(5));

        group.MapGet("/following", GetFollowing)
            .WithName("GetFollowingV2")
            .WithCacheableResponse(TimeSpan.FromMinutes(5));

        // Post endpoints with batch operations
        group.MapPost("/posts", CreatePost)
            .WithName("CreatePostV2")
            .Accepts<CreatePostRequest>("application/json")
            .Produces<PostDetailDto>(StatusCodes.Status201Created);

        group.MapDelete("/posts/{postId}", DeletePost)
            .WithName("DeletePostV2");

        group.MapGet("/posts", GetUserPosts)
            .WithName("GetUserPostsV2")
            .WithCacheableResponse(TimeSpan.FromMinutes(5));

        group.MapGet("/posts/{postId}", GetPostDetail)
            .WithName("GetPostDetailV2");

        // Post interaction endpoints
        group.MapPost("/posts/{postId}/like", LikePost)
            .WithName("LikePostV2");

        group.MapDelete("/posts/{postId}/like", UnlikePost)
            .WithName("UnlikePostV2");

        group.MapPost("/posts/{postId}/comment", CommentOnPost)
            .WithName("CommentOnPostV2");

        group.MapDelete("/posts/{postId}/comments/{commentId}", DeleteComment)
            .WithName("DeleteCommentV2");

        group.MapPost("/posts/{postId}/share", SharePost)
            .WithName("SharePostV2");

        // Feed endpoints
        group.MapGet("/feed", GetFeed)
            .WithName("GetFeedV2")
            .WithCacheableResponse(TimeSpan.FromMinutes(2));

        group.MapGet("/activity", GetActivityFeed)
            .WithName("GetActivityFeedV2")
            .WithCacheableResponse(TimeSpan.FromMinutes(5));

        // Recommendations and search
        group.MapGet("/recommendations", GetRecommendedConnections)
            .WithName("GetRecommendedConnectionsV2")
            .WithCacheableResponse(TimeSpan.FromHours(1));

        group.MapGet("/search", SearchUsers)
            .WithName("SearchUsersV2");

        // Analytics
        group.MapGet("/analytics/profile/{userId}", GetProfileAnalytics)
            .WithName("GetProfileAnalyticsV2")
            .WithCacheableResponse(TimeSpan.FromMinutes(30));

        group.MapGet("/analytics/posts", GetPostsAnalytics)
            .WithName("GetPostsAnalyticsV2")
            .WithCacheableResponse(TimeSpan.FromMinutes(10));
    }

    private static async Task<IResult> GetProfile(
        HttpContext context,
        ISocialNetworkService service,
        IQueryBus queryBus)
    {
        var userId = GetUserId(context);
        var query = new GetUserProfileQuery { UserId = userId };
        var profile = await queryBus.SendAsync<GetUserProfileQuery, UserProfileDto>(query);
        return Results.Ok(profile);
    }

    private static async Task<IResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        var command = new UpdateProfileCommand
        {
            UserId = userId,
            Name = request.Name,
            Bio = request.Bio,
            ProfileImageUrl = request.ProfileImageUrl,
            Location = request.Location
        };
        await commandBus.SendAsync(command);
        return Results.Ok(new { message = "Profile updated successfully" });
    }

    private static async Task<IResult> AddConnection(
        [FromBody] AddConnectionRequest request,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        var command = new AddConnectionCommand
        {
            UserId = userId,
            ConnectedUserId = request.ConnectedUserId,
            Type = request.Type,
            Note = request.Note
        };
        await commandBus.SendAsync(command);
        return Results.Ok(new { message = "Connection added successfully" });
    }

    private static async Task<IResult> RemoveConnection(
        Guid connectedUserId,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        await commandBus.SendAsync(new RemoveConnectionCommand
        {
            UserId = userId,
            ConnectedUserId = connectedUserId
        });
        return Results.Ok(new { message = "Connection removed successfully" });
    }

    private static async Task<IResult> GetConnections(
        HttpContext context,
        IQueryBus queryBus)
    {
        var userId = GetUserId(context);
        var query = new GetConnectionsQuery { UserId = userId };
        var connections = await queryBus.SendAsync<GetConnectionsQuery, List<SocialConnectionDto>>(query);
        return Results.Ok(connections);
    }

    private static async Task<IResult> FollowUser(
        Guid targetUserId,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        await commandBus.SendAsync(new FollowUserCommand
        {
            UserId = userId,
            TargetUserId = targetUserId
        });
        return Results.Ok(new { message = "User followed successfully" });
    }

    private static async Task<IResult> UnfollowUser(
        Guid targetUserId,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        await commandBus.SendAsync(new UnfollowUserCommand
        {
            UserId = userId,
            TargetUserId = targetUserId
        });
        return Results.Ok(new { message = "User unfollowed successfully" });
    }

    private static async Task<IResult> GetFollowers(
        HttpContext context,
        IQueryBus queryBus,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var userId = GetUserId(context);
        var query = new GetFollowersQuery { UserId = userId, Skip = skip, Take = take };
        var followers = await queryBus.SendAsync<GetFollowersQuery, List<SocialNetworkDto>>(query);
        return Results.Ok(followers);
    }

    private static async Task<IResult> GetFollowing(
        HttpContext context,
        IQueryBus queryBus,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var userId = GetUserId(context);
        var query = new GetFollowingQuery { UserId = userId, Skip = skip, Take = take };
        var following = await queryBus.SendAsync<GetFollowingQuery, List<SocialNetworkDto>>(query);
        return Results.Ok(following);
    }

    private static async Task<IResult> CreatePost(
        [FromBody] CreatePostRequest request,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        var command = new CreatePostCommand
        {
            UserId = userId,
            Content = request.Content,
            Tags = request.Tags ?? new(),
            AttachmentUrls = request.AttachmentUrls ?? new()
        };
        var postId = await commandBus.SendAsync<CreatePostCommand, Guid>(command);
        return Results.Created($"/api/v2/social/posts/{postId}", new { postId });
    }

    private static async Task<IResult> DeletePost(
        Guid postId,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        await commandBus.SendAsync(new DeletePostCommand
        {
            UserId = userId,
            PostId = postId
        });
        return Results.Ok(new { message = "Post deleted successfully" });
    }

    private static async Task<IResult> GetUserPosts(
        HttpContext context,
        IQueryBus queryBus,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var userId = GetUserId(context);
        var query = new GetUserPostsQuery { UserId = userId, Skip = skip, Take = take };
        var posts = await queryBus.SendAsync<GetUserPostsQuery, List<UserPostDto>>(query);
        return Results.Ok(new { posts });
    }

    private static async Task<IResult> GetPostDetail(
        Guid postId,
        HttpContext context,
        IQueryBus queryBus)
    {
        var userId = GetUserId(context);
        var query = new GetPostDetailQuery { UserId = userId, PostId = postId };
        var post = await queryBus.SendAsync<GetPostDetailQuery, PostDetailDto>(query);
        return Results.Ok(post);
    }

    private static async Task<IResult> LikePost(
        Guid postId,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        await commandBus.SendAsync(new LikePostCommand { UserId = userId, PostId = postId });
        return Results.Ok(new { message = "Post liked" });
    }

    private static async Task<IResult> UnlikePost(
        Guid postId,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        var command = new UnlikePostCommand { UserId = userId, PostId = postId };
        await commandBus.SendAsync(command);
        return Results.Ok(new { message = "Post unliked" });
    }

    private static async Task<IResult> CommentOnPost(
        Guid postId,
        [FromBody] CommentRequest request,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        await commandBus.SendAsync(new CommentOnPostCommand
        {
            UserId = userId,
            PostId = postId,
            CommentText = request.Text
        });
        return Results.Ok(new { message = "Comment added" });
    }

    private static async Task<IResult> DeleteComment(
        Guid postId,
        Guid commentId,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        await commandBus.SendAsync(new DeleteCommentCommand
        {
            UserId = userId,
            PostId = postId,
            CommentId = commentId
        });
        return Results.Ok(new { message = "Comment deleted" });
    }

    private static async Task<IResult> SharePost(
        Guid postId,
        [FromBody] ShareRequest? request,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = GetUserId(context);
        await commandBus.SendAsync(new SharePostCommand
        {
            UserId = userId,
            PostId = postId,
            PersonalMessage = request?.PersonalMessage
        });
        return Results.Ok(new { message = "Post shared" });
    }

    private static async Task<IResult> GetFeed(
        HttpContext context,
        IQueryBus queryBus,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var userId = GetUserId(context);
        var query = new GetFeedQuery { UserId = userId, Skip = skip, Take = take };
        var feed = await queryBus.SendAsync<GetFeedQuery, List<UserPostDto>>(query);
        return Results.Ok(feed);
    }

    private static async Task<IResult> GetActivityFeed(
        HttpContext context,
        IQueryBus queryBus,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var userId = GetUserId(context);
        var query = new GetActivityFeedQuery { UserId = userId, Skip = skip, Take = take };
        var activities = await queryBus.SendAsync<GetActivityFeedQuery, List<UserActivityDto>>(query);
        return Results.Ok(activities);
    }

    private static async Task<IResult> GetRecommendedConnections(
        HttpContext context,
        IQueryBus queryBus,
        [FromQuery] int topN = 10)
    {
        var userId = GetUserId(context);
        var query = new GetRecommendedConnectionsQuery { UserId = userId, TopN = topN };
        var recommendations = await queryBus.SendAsync<GetRecommendedConnectionsQuery, List<SocialNetworkDto>>(query);
        return Results.Ok(recommendations);
    }

    private static async Task<IResult> SearchUsers(
        HttpContext context,
        IQueryBus queryBus,
        [FromQuery] string q,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var userId = GetUserId(context);
        var query = new SearchUsersQuery { UserId = userId, SearchTerm = q, Skip = skip, Take = take };
        var results = await queryBus.SendAsync<SearchUsersQuery, List<UserSearchResultDto>>(query);
        return Results.Ok(results);
    }

    private static async Task<IResult> GetProfileAnalytics(
        Guid userId,
        HttpContext context,
        IQueryBus queryBus)
    {
        var currentUserId = GetUserId(context);
        var query = new GetProfileAnalyticsQuery { UserId = userId, CurrentUserId = currentUserId };
        var analytics = await queryBus.SendAsync<GetProfileAnalyticsQuery, ProfileAnalyticsDto>(query);
        return Results.Ok(analytics);
    }

    private static async Task<IResult> GetPostsAnalytics(
        HttpContext context,
        IQueryBus queryBus)
    {
        var userId = GetUserId(context);
        var query = new GetPostsAnalyticsQuery { UserId = userId };
        var analytics = await queryBus.SendAsync<GetPostsAnalyticsQuery, PostsAnalyticsDto>(query);
        return Results.Ok(analytics);
    }

    private static Guid GetUserId(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User not authenticated");
        return Guid.Parse(userIdClaim);
    }
}