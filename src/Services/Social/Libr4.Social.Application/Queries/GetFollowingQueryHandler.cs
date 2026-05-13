using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetFollowingQuery : IQuery<List<SocialNetworkDto>>
{
    public Guid UserId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

public class GetFollowingQueryHandler : IQueryHandler<GetFollowingQuery, List<SocialNetworkDto>>
{
    private readonly ISocialNetworkRepository _repository;

    public GetFollowingQueryHandler(ISocialNetworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SocialNetworkDto>> HandleAsync(GetFollowingQuery query, CancellationToken cancellationToken)
    {
        var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (network == null)
            return new List<SocialNetworkDto>();

        var followingIds = network.Following.Skip(query.Skip).Take(query.Take).ToList();
        var following = new List<SocialNetworkDto>();

        foreach (var followingId in followingIds)
        {
            var followingNetwork = await _repository.GetByUserIdAsync(followingId, cancellationToken);
            if (followingNetwork != null)
            {
                following.Add(new SocialNetworkDto(
                    followingNetwork.Id,
                    followingNetwork.UserId,
                    followingNetwork.Connections.Select(c => new SocialConnectionDto(c.Id, c.ConnectedUserId, c.Type, c.Note)).ToList(),
                    followingNetwork.Followers,
                    followingNetwork.Following,
                    new UserProfileDto(followingNetwork.Profile.Name, followingNetwork.Profile.Bio, followingNetwork.Profile.ProfileImageUrl, followingNetwork.Profile.Location),
                    followingNetwork.Posts.Select(p => new UserPostDto(p.Id, p.Content, p.Tags, p.Likes.Count, p.Comments.Count, p.CreatedAt)).ToList(),
                    followingNetwork.Followers.Count,
                    followingNetwork.Following.Count));
            }
        }

        return following;
    }
}
