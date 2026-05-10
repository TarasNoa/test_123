using System;
using System.Collections.Generic;
using System.Linq;
using Libr4.Shared.Kernel.Domain;
using Libr4.Social.Domain.Events;

namespace Libr4.Social.Domain.Network;

public enum ConnectionType
{
    Friend,
    Colleague,
    Family,
    Acquaintance,
    Following
}

public enum PrivacyLevel
{
    Public,
    FriendsOnly,
    Private
}

public class SocialProfile : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public PrivacyLevel PrivacyLevel { get; private set; }
    public int FollowerCount { get; private set; }
    public int FollowingCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<SocialConnection> _connections = new();
    public IReadOnlyCollection<SocialConnection> Connections => _connections.AsReadOnly();

    private readonly List<SocialPost> _posts = new();
    public IReadOnlyCollection<SocialPost> Posts => _posts.AsReadOnly();

    private SocialProfile() { }

    public static SocialProfile Create(Guid userId, string displayName, PrivacyLevel privacyLevel = PrivacyLevel.Public)
    {
        return new SocialProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = displayName,
            PrivacyLevel = privacyLevel,
            FollowerCount = 0,
            FollowingCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string displayName, string? bio = null, string? avatarUrl = null)
    {
        DisplayName = displayName;
        Bio = bio;
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrivacyLevel(PrivacyLevel privacyLevel)
    {
        PrivacyLevel = privacyLevel;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddConnection(SocialConnection connection)
    {
        _connections.Add(connection);
        if (connection.Type == ConnectionType.Following)
        {
            FollowingCount++;
        }
        else if (connection.Type == ConnectionType.Friend)
        {
            FollowerCount++;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveConnection(Guid connectionId)
    {
        var connection = _connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection != null)
        {
            _connections.Remove(connection);
            if (connection.Type == ConnectionType.Following)
            {
                FollowingCount--;
            }
            else if (connection.Type == ConnectionType.Friend)
            {
                FollowerCount--;
            }
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddPost(SocialPost post)
    {
        _posts.Add(post);
        UpdatedAt = DateTime.UtcNow;
    }
}

public class SocialConnection : Entity<Guid>
{
    public Guid ProfileId { get; private set; }
    public Guid ConnectedProfileId { get; private set; }
    public ConnectionType Type { get; private set; }
    public DateTime ConnectedAt { get; private set; }
    public bool IsActive { get; private set; }

    private SocialConnection() { }

    public static SocialConnection Create(Guid profileId, Guid connectedProfileId, ConnectionType type)
    {
        return new SocialConnection
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            ConnectedProfileId = connectedProfileId,
            Type = type,
            ConnectedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void UpdateType(ConnectionType type)
    {
        Type = type;
    }
}

public class SocialPost : Entity<Guid>
{
    public Guid ProfileId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public PrivacyLevel PrivacyLevel { get; private set; }
    public int LikeCount { get; private set; }
    public int CommentCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<SocialInteraction> _interactions = new();
    public IReadOnlyCollection<SocialInteraction> Interactions => _interactions.AsReadOnly();

    private SocialPost() { }

    public static SocialPost Create(Guid profileId, string content, PrivacyLevel privacyLevel = PrivacyLevel.Public)
    {
        return new SocialPost
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            Content = content,
            PrivacyLevel = privacyLevel,
            LikeCount = 0,
            CommentCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddInteraction(SocialInteraction interaction)
    {
        _interactions.Add(interaction);
        if (interaction.Type == InteractionType.Like)
        {
            LikeCount++;
        }
        else if (interaction.Type == InteractionType.Comment)
        {
            CommentCount++;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveInteraction(Guid interactionId)
    {
        var interaction = _interactions.FirstOrDefault(i => i.Id == interactionId);
        if (interaction != null)
        {
            _interactions.Remove(interaction);
            if (interaction.Type == InteractionType.Like)
            {
                LikeCount--;
            }
            else if (interaction.Type == InteractionType.Comment)
            {
                CommentCount--;
            }
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateContent(string content)
    {
        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class SocialInteraction : Entity<Guid>
{
    public Guid PostId { get; private set; }
    public Guid ProfileId { get; private set; }
    public InteractionType Type { get; private set; }
    public string? Content { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SocialInteraction() { }

    public static SocialInteraction Create(Guid postId, Guid profileId, InteractionType type, string? content = null)
    {
        return new SocialInteraction
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            ProfileId = profileId,
            Type = type,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum InteractionType
{
    Like,
    Comment,
    Share,
    Bookmark
}

public class SocialNetwork : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public List<SocialConnection> Connections { get; private set; } = new();
    public List<Guid> Followers { get; private set; } = new();
    public List<Guid> Following { get; private set; } = new();
    public UserProfile Profile { get; private set; } = new();
    public List<UserPost> Posts { get; private set; } = new();
    public List<UserActivity> ActivityFeed { get; private set; } = new();
    public DateTime CreatedAt { get; private set; }

    private SocialNetwork() { }

    public static SocialNetwork Create(Guid userId)
    {
        var network = new SocialNetwork
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        network.RaiseDomainEvent(new SocialNetworkCreatedEvent(network.Id, userId, network.CreatedAt));
        return network;
    }

    public void AddConnection(Guid connectedUserId, ConnectionType type, string? note = null)
    {
        if (Connections.Any(c => c.ConnectedUserId == connectedUserId))
            return;

        var connection = new SocialConnection(Guid.NewGuid(), connectedUserId, type, note, DateTime.UtcNow);
        Connections.Add(connection);

        if (type == ConnectionType.Following)
        {
            Following.Add(connectedUserId);
        }

        RaiseDomainEvent(new ConnectionAddedEvent(Id, connectedUserId, type));
    }

    public void RemoveConnection(Guid connectedUserId)
    {
        var connection = Connections.FirstOrDefault(c => c.ConnectedUserId == connectedUserId);
        if (connection != null)
        {
            Connections.Remove(connection);
            Following.Remove(connectedUserId);
            RaiseDomainEvent(new ConnectionRemovedEvent(Id, connectedUserId));
        }
    }

    public void AddFollower(Guid followerId)
    {
        if (!Followers.Contains(followerId))
        {
            Followers.Add(followerId);
            RaiseDomainEvent(new FollowerAddedEvent(Id, followerId));
        }
    }

    public void RemoveFollower(Guid followerId)
    {
        if (Followers.Remove(followerId))
        {
            RaiseDomainEvent(new FollowerRemovedEvent(Id, followerId));
        }
    }

    public void UpdateProfile(string name, string? bio, string? profileImageUrl, string? location)
    {
        Profile = new UserProfile(name, bio, profileImageUrl, location);
        RaiseDomainEvent(new ProfileUpdatedEvent(Id, name, bio));
    }

    public void CreatePost(string content, List<string>? tags = null, List<string>? attachmentUrls = null)
    {
        var post = new UserPost(
            Guid.NewGuid(),
            content,
            tags ?? new List<string>(),
            attachmentUrls ?? new List<string>(),
            DateTime.UtcNow
        );
        Posts.Add(post);
        ActivityFeed.Add(new UserActivity(Guid.NewGuid(), $"Created post: {content.Substring(0, Math.Min(50, content.Length))}", DateTime.UtcNow, ActivityType.PostCreated));
        RaiseDomainEvent(new PostCreatedEvent(Id, post.Id, content, tags));
    }

    public void DeletePost(Guid postId)
    {
        var post = Posts.FirstOrDefault(p => p.Id == postId);
        if (post != null)
        {
            Posts.Remove(post);
            ActivityFeed.Add(new UserActivity(Guid.NewGuid(), "Deleted a post", DateTime.UtcNow, ActivityType.PostDeleted));
            RaiseDomainEvent(new PostDeletedEvent(Id, postId));
        }
    }

    public void LikePost(Guid postId, Guid likerUserId)
    {
        var post = Posts.FirstOrDefault(p => p.Id == postId);
        if (post != null && !post.Likes.Contains(likerUserId))
        {
            post.Likes.Add(likerUserId);
            RaiseDomainEvent(new PostLikedEvent(Id, postId, likerUserId));
        }
    }

    public void CommentOnPost(Guid postId, Guid commenterUserId, string commentText)
    {
        var post = Posts.FirstOrDefault(p => p.Id == postId);
        if (post != null)
        {
            var comment = new PostComment(Guid.NewGuid(), commenterUserId, commentText, DateTime.UtcNow);
            post.Comments.Add(comment);
            RaiseDomainEvent(new PostCommentedEvent(Id, postId, commenterUserId, commentText));
        }
    }

    public void SharePost(Guid postId, string? personalMessage = null)
    {
        var post = Posts.FirstOrDefault(p => p.Id == postId);
        if (post != null)
        {
            post.Shares.Add(new PostShare(Guid.NewGuid(), UserId, personalMessage, DateTime.UtcNow));
            ActivityFeed.Add(new UserActivity(Guid.NewGuid(), "Shared a post", DateTime.UtcNow, ActivityType.PostShared));
            RaiseDomainEvent(new PostSharedEvent(Id, postId));
        }
    }

    public void AddToActivityFeed(string description, ActivityType activityType)
    {
        var activity = new UserActivity(Guid.NewGuid(), description, DateTime.UtcNow, activityType);
        ActivityFeed.Add(activity);
    }
}

public enum ConnectionType { Friend, Following, Colleague, Blocked }
public enum ActivityType { PostCreated, PostDeleted, PostShared, PostLiked, Follow, Unfollow, CommentedOnPost }

public record SocialConnection(Guid Id, Guid ConnectedUserId, ConnectionType Type, string? Note, DateTime ConnectedAt);

public record UserProfile(string Name, string? Bio, string? ProfileImageUrl, string? Location);

public record UserPost(Guid Id, string Content, List<string> Tags, List<string> AttachmentUrls, DateTime CreatedAt)
{
    public List<Guid> Likes { get; set; } = new();
    public List<PostComment> Comments { get; set; } = new();
    public List<PostShare> Shares { get; set; } = new();
}

public record PostComment(Guid Id, Guid AuthorId, string Text, DateTime CreatedAt);

public record PostShare(Guid Id, Guid SharedByUserId, string? PersonalMessage, DateTime SharedAt);

public record UserActivity(Guid Id, string Description, DateTime Timestamp, ActivityType Type);
