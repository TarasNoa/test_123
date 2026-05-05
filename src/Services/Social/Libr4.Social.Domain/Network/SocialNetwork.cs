using Libr4.Shared.Kernel.Domain;

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
