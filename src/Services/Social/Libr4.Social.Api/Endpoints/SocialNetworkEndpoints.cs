using Libr4.Social.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Libr4.Social.Api.Endpoints;

public static class SocialNetworkEndpoints
{
    public static void MapSocialNetworkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/social")
            .WithTags("Social Network")
            .RequireAuthorization();

        // Profile
        group.MapGet("/profile", GetProfile)
            .WithName("GetProfile")
            .WithSummary("Get user profile");

        group.MapPut("/profile", UpdateProfile)
            .WithName("UpdateProfile")
            .WithSummary("Update user profile");

        // Connections
        group.MapPost("/connections", AddConnection)
            .WithName("AddConnection")
            .WithSummary("Add a social connection");

        group.MapDelete("/connections/{connectedUserId}", RemoveConnection)
            .WithName("RemoveConnection")
            .WithSummary("Remove a social connection");

        group.MapGet("/connections", GetConnections)
            .WithName("GetConnections")
            .WithSummary("Get user connections");

        // Follow
        group.MapPost("/follow/{targetUserId}", FollowUser)
            .WithName("FollowUser")
            .WithSummary("Follow a user");

        group.MapDelete("/follow/{targetUserId}", UnfollowUser)
            .WithName("UnfollowUser")
            .WithSummary("Unfollow a user");

        group.MapGet("/followers", GetFollowers)
            .WithName("GetFollowers")
            .WithSummary("Get user followers");

        group.MapGet("/following", GetFollowing)
            .WithName("GetFollowing")
            .WithSummary("Get users being followed");

        // Posts
        group.MapPost("/posts", CreatePost)
            .WithName("CreatePost")
            .WithSummary("Create a new post");

        group.MapDelete("/posts/{postId}", DeletePost)
            .WithName("DeletePost")
            .WithSummary("Delete a post");

        group.MapGet("/posts", GetUserPosts)
            .WithName("GetUserPosts")
            .WithSummary("Get user posts");

        group.MapGet("/posts/{postId}", GetPostDetail)
            .WithName("GetPostDetail")
            .WithSummary("Get post details");

        // Post interactions
        group.MapPost("/posts/{postId}/like", LikePost)
            .WithName("LikePost")
            .WithSummary("Like a post");

        group.MapDelete("/posts/{postId}/like", UnlikePost)
            .WithName("UnlikePost")
            .WithSummary("Unlike a post");

        group.MapPost("/posts/{postId}/comment", CommentOnPost)
            .WithName("CommentOnPost")
            .WithSummary("Comment on a post");

        group.MapPost("/posts/{postId}/share", SharePost)
            .WithName("SharePost")
            .WithSummary("Share a post");

        // Feed
        group.MapGet("/feed", GetFeed)
            .WithName("GetFeed")
            .WithSummary("Get user feed");

        group.MapGet("/activity", GetActivityFeed)
            .WithName("GetActivityFeed")
            .WithSummary("Get user activity feed");

        // Recommendations
        group.MapGet("/recommendations", GetRecommendedConnections)
            .WithName("GetRecommendedConnections")
            .WithSummary("Get recommended connections");
    }

    private static async Task<IResult> GetProfile(HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var network = await service.GetNetworkAsync(userId);
        return Results.Ok(new { profile = network.Profile });
    }

    private static async Task<IResult> UpdateProfile([FromBody] UpdateProfileRequest request, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var network = await service.UpdateProfileAsync(userId, request);
        return Results.Ok(new { profile = network.Profile });
    }

    private static async Task<IResult> AddConnection([FromBody] AddConnectionRequest request, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await service.AddConnectionAsync(userId, request);
        return Results.Ok(new { message = "Connection added" });
    }

    private static async Task<IResult> RemoveConnection(Guid connectedUserId, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await service.RemoveConnectionAsync(userId, connectedUserId);
        return Results.Ok(new { message = "Connection removed" });
    }

    private static async Task<IResult> GetConnections(HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var connections = await service.GetConnectionsAsync(userId);
        return Results.Ok(new { connections });
    }

    private static async Task<IResult> FollowUser(Guid targetUserId, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await service.FollowUserAsync(userId, targetUserId);
        return Results.Ok(new { message = "User followed" });
    }

    private static async Task<IResult> UnfollowUser(Guid targetUserId, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await service.UnfollowUserAsync(userId, targetUserId);
        return Results.Ok(new { message = "User unfollowed" });
    }

    private static async Task<IResult> GetFollowers(HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var followers = await service.GetFollowersAsync(userId);
        return Results.Ok(new { followers });
    }

    private static async Task<IResult> GetFollowing(HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var following = await service.GetFollowingAsync(userId);
        return Results.Ok(new { following });
    }

    private static async Task<IResult> CreatePost([FromBody] CreatePostRequest request, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var post = await service.CreatePostAsync(userId, request);
        return Results.Created($"/api/social/posts/{post.Id}", new { post });
    }

    private static async Task<IResult> DeletePost(Guid postId, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await service.DeletePostAsync(userId, postId);
        return Results.Ok(new { message = "Post deleted" });
    }

    private static async Task<IResult> GetUserPosts([FromQuery] int skip = 0, [FromQuery] int take = 20, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var posts = await service.GetUserPostsAsync(userId, skip, take);
        return Results.Ok(new { posts });
    }

    private static async Task<IResult> GetPostDetail(Guid postId, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var post = await service.GetPostDetailAsync(userId, postId);
        return Results.Ok(new { post });
    }

    private static async Task<IResult> LikePost(Guid postId, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await service.LikePostAsync(userId, postId);
        return Results.Ok(new { message = "Post liked" });
    }

    private static async Task<IResult> UnlikePost(Guid postId, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await service.UnlikePostAsync(userId, postId);
        return Results.Ok(new { message = "Post unliked" });
    }

    private static async Task<IResult> CommentOnPost(Guid postId, [FromBody] CommentRequest request, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await service.CommentOnPostAsync(userId, postId, request.Text);
        return Results.Ok(new { message = "Comment added" });
    }

    private static async Task<IResult> SharePost(Guid postId, [FromBody] ShareRequest? request, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        await service.SharePostAsync(userId, postId, request?.PersonalMessage);
        return Results.Ok(new { message = "Post shared" });
    }

    private static async Task<IResult> GetFeed([FromQuery] int skip = 0, [FromQuery] int take = 20, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var feed = await service.GetFeedAsync(userId, skip, take);
        return Results.Ok(new { feed });
    }

    private static async Task<IResult> GetActivityFeed([FromQuery] int skip = 0, [FromQuery] int take = 50, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var activities = await service.GetActivityFeedAsync(userId, skip, take);
        return Results.Ok(new { activities });
    }

    private static async Task<IResult> GetRecommendedConnections([FromQuery] int topN = 10, HttpContext context, ISocialNetworkService service)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var recommendations = await service.GetRecommendedConnectionsAsync(userId, topN);
        return Results.Ok(new { recommendations });
    }
}

public record CommentRequest(string Text);
public record ShareRequest(string? PersonalMessage);