using Libr4.Social.Application.Abstractions;
using Libr4.Social.Domain.Network;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application;

public class SocialNetworkService : ISocialNetworkService
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ILogger<SocialNetworkService> _logger;

    public SocialNetworkService(ISocialNetworkRepository repository, ILogger<SocialNetworkService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<SocialNetworkDto> GetNetworkAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        return MapToDto(network);
    }

    public async Task<SocialNetworkDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        network.UpdateProfile(request.Name, request.Bio, request.ProfileImageUrl, request.Location);
        await _repository.UpdateAsync(network, cancellationToken);

        _logger.LogInformation("Profile updated for user {UserId}", userId);
        return MapToDto(network);
    }

    public async Task AddConnectionAsync(Guid userId, AddConnectionRequest request, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        network.AddConnection(request.ConnectedUserId, request.Type, request.Note);
        await _repository.UpdateAsync(network, cancellationToken);

        _logger.LogInformation("Connection added for user {UserId} to {ConnectedUserId}", userId, request.ConnectedUserId);
    }

    public async Task RemoveConnectionAsync(Guid userId, Guid connectedUserId, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        network.RemoveConnection(connectedUserId);
        await _repository.UpdateAsync(network, cancellationToken);

        _logger.LogInformation("Connection removed for user {UserId} from {ConnectedUserId}", userId, connectedUserId);
    }

    public async Task<List<SocialConnectionDto>> GetConnectionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        return network.Connections
            .Select(c => new SocialConnectionDto(c.Id, c.ConnectedUserId, c.Type, c.Note))
            .ToList();
    }

    public async Task FollowUserAsync(Guid userId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        network.AddConnection(targetUserId, ConnectionType.Following);

        var targetNetwork = await _repository.GetByUserIdAsync(targetUserId, cancellationToken);
        if (targetNetwork != null)
        {
            targetNetwork.AddFollower(userId);
            await _repository.UpdateAsync(targetNetwork, cancellationToken);
        }

        await _repository.UpdateAsync(network, cancellationToken);
        _logger.LogInformation("User {UserId} followed {TargetUserId}", userId, targetUserId);
    }

    public async Task UnfollowUserAsync(Guid userId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        network.RemoveConnection(targetUserId);

        var targetNetwork = await _repository.GetByUserIdAsync(targetUserId, cancellationToken);
        if (targetNetwork != null)
        {
            targetNetwork.RemoveFollower(userId);
            await _repository.UpdateAsync(targetNetwork, cancellationToken);
        }

        await _repository.UpdateAsync(network, cancellationToken);
    }

    public async Task<List<Guid>> GetFollowersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        return network.Followers;
    }

    public async Task<List<Guid>> GetFollowingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        return network.Following;
    }

    public async Task<UserPostDto> CreatePostAsync(Guid userId, CreatePostRequest request, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        network.CreatePost(request.Content, request.Tags, request.AttachmentUrls);
        await _repository.UpdateAsync(network, cancellationToken);

        var post = network.Posts.Last();
        _logger.LogInformation("Post created by user {UserId}", userId);

        return new UserPostDto(post.Id, post.Content, post.Tags, 0, 0, post.CreatedAt);
    }

    public async Task DeletePostAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        network.DeletePost(postId);
        await _repository.UpdateAsync(network, cancellationToken);
    }

    public async Task<List<UserPostDto>> GetUserPostsAsync(Guid userId, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        return network.Posts
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(p => new UserPostDto(p.Id, p.Content, p.Tags, p.Likes.Count, p.Comments.Count, p.CreatedAt))
            .ToList();
    }

    public async Task<PostDetailDto> GetPostDetailAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        var post = network.Posts.FirstOrDefault(p => p.Id == postId);
        if (post == null) throw new InvalidOperationException("Post not found");

        return new PostDetailDto(
            post.Id,
            post.Content,
            post.Tags,
            post.Likes,
            post.Comments.Select(c => new PostCommentDto(c.Id, c.AuthorId, c.Text, c.CreatedAt)).ToList(),
            post.CreatedAt
        );
    }

    public async Task LikePostAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        network.LikePost(postId, userId);
        await _repository.UpdateAsync(network, cancellationToken);
    }

    public async Task UnlikePostAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        var post = network.Posts.FirstOrDefault(p => p.Id == postId);
        if (post != null)
        {
            post.Likes.Remove(userId);
            await _repository.UpdateAsync(network, cancellationToken);
        }
    }

    public async Task CommentOnPostAsync(Guid userId, Guid postId, string commentText, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        network.CommentOnPost(postId, userId, commentText);
        await _repository.UpdateAsync(network, cancellationToken);
    }

    public async Task SharePostAsync(Guid userId, Guid postId, string? personalMessage = null, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        network.SharePost(postId, personalMessage);
        await _repository.UpdateAsync(network, cancellationToken);
    }

    public async Task<List<UserActivityDto>> GetActivityFeedAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        return network.ActivityFeed
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .Select(a => new UserActivityDto(a.Description, a.Timestamp, a.Type))
            .ToList();
    }

    public async Task<List<UserPostDto>> GetFeedAsync(Guid userId, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) throw new InvalidOperationException("Social network not found");

        var timelinePostIds = new List<Guid>();

        foreach (var followingUserId in network.Following)
        {
            var followingNetwork = await _repository.GetByUserIdAsync(followingUserId, cancellationToken);
            if (followingNetwork != null)
            {
                timelinePostIds.AddRange(followingNetwork.Posts.Select(p => p.Id));
            }
        }

        timelinePostIds.AddRange(network.Posts.Select(p => p.Id));

        return timelinePostIds
            .Distinct()
            .OrderByDescending(id => id)
            .Skip(skip)
            .Take(take)
            .Select(id => new UserPostDto(id, "", new List<string>(), 0, 0, DateTime.UtcNow))
            .ToList();
    }

    public async Task<List<SocialNetworkDto>> GetRecommendedConnectionsAsync(Guid userId, int topN = 10, CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (network == null) return new List<SocialNetworkDto>();

        var allUsers = await _repository.GetAllAsync(cancellationToken);
        var potentialConnections = allUsers
            .Where(u => u.UserId != userId && !network.Connections.Any(c => c.ConnectedUserId == u.UserId))
            .OrderByDescending(u => u.Followers.Count)
            .Take(topN)
            .ToList();

        return potentialConnections.Select(MapToDto).ToList();
    }

    private SocialNetworkDto MapToDto(SocialNetwork network)
    {
        return new SocialNetworkDto(
            network.Id,
            network.UserId,
            network.Connections.Select(c => new SocialConnectionDto(c.Id, c.ConnectedUserId, c.Type, c.Note)).ToList(),
            network.Followers,
            network.Following,
            new UserProfileDto(network.Profile.Name, network.Profile.Bio, network.Profile.ProfileImageUrl, network.Profile.Location),
            network.Posts.Select(p => new UserPostDto(p.Id, p.Content, p.Tags, p.Likes.Count, p.Comments.Count, p.CreatedAt)).ToList(),
            network.Followers.Count,
            network.Following.Count
        );
    }
}