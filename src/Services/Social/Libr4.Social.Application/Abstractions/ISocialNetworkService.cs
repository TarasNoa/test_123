using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libr4.Social.Domain.Network;

namespace Libr4.Social.Application.Abstractions;

public record SocialNetworkDto(
    Guid Id,
    Guid UserId,
    List<SocialConnectionDto> Connections,
    List<Guid> Followers,
    List<Guid> Following,
    UserProfileDto Profile,
    List<UserPostDto> Posts,
    int FollowerCount,
    int FollowingCount);

public record SocialConnectionDto(Guid Id, Guid ConnectedUserId, ConnectionType Type, string? Note);

public record UserProfileDto(string Name, string? Bio, string? ProfileImageUrl, string? Location);

public record UserPostDto(
    Guid Id,
    string Content,
    List<string> Tags,
    int LikesCount,
    int CommentsCount,
    DateTime CreatedAt,
    bool IsLikedByCurrentUser = false,
    List<PostCommentDto>? Comments = null);

public record PostDetailDto(
    Guid Id,
    string Content,
    List<string> Tags,
    List<Guid> Likes,
    List<PostCommentDto> Comments,
    DateTime CreatedAt);

public record PostCommentDto(Guid Id, Guid AuthorId, string Text, DateTime CreatedAt);

public record UserActivityDto(string Description, DateTime Timestamp, ActivityType ActivityType);

public record CreatePostRequest(string Content, List<string>? Tags, List<string>? AttachmentUrls);
public record UpdateProfileRequest(string Name, string? Bio, string? ProfileImageUrl, string? Location);
public record AddConnectionRequest(Guid ConnectedUserId, ConnectionType Type, string? Note);
public record CommentRequest(string Text);
public record ShareRequest(string? PersonalMessage);

public interface ISocialNetworkService
{
    Task<SocialNetworkDto> GetNetworkAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SocialNetworkDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    
    Task AddConnectionAsync(Guid userId, AddConnectionRequest request, CancellationToken cancellationToken = default);
    Task RemoveConnectionAsync(Guid userId, Guid connectedUserId, CancellationToken cancellationToken = default);
    Task<List<SocialConnectionDto>> GetConnectionsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    Task FollowUserAsync(Guid userId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task UnfollowUserAsync(Guid userId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetFollowersAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetFollowingAsync(Guid userId, CancellationToken cancellationToken = default);
    
    Task<UserPostDto> CreatePostAsync(Guid userId, CreatePostRequest request, CancellationToken cancellationToken = default);
    Task DeletePostAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);
    Task<List<UserPostDto>> GetUserPostsAsync(Guid userId, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<PostDetailDto> GetPostDetailAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);
    
    Task LikePostAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);
    Task UnlikePostAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);
    Task CommentOnPostAsync(Guid userId, Guid postId, string commentText, CancellationToken cancellationToken = default);
    Task SharePostAsync(Guid userId, Guid postId, string? personalMessage = null, CancellationToken cancellationToken = default);
    
    Task<List<UserActivityDto>> GetActivityFeedAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<List<UserPostDto>> GetFeedAsync(Guid userId, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    
    Task<List<SocialNetworkDto>> GetRecommendedConnectionsAsync(Guid userId, int topN = 10, CancellationToken cancellationToken = default);
}