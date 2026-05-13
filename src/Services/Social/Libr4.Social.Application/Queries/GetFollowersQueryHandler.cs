using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetFollowersQuery : IQuery<List<SocialNetworkDto>>
{
    public Guid UserId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

public class GetFollowersQueryHandler : IQueryHandler<GetFollowersQuery, List<SocialNetworkDto>>
{
    private readonly ISocialNetworkRepository _repository;

    public GetFollowersQueryHandler(ISocialNetworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SocialNetworkDto>> HandleAsync(GetFollowersQuery query, CancellationToken cancellationToken)
    {
        var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (network == null)
            return new List<SocialNetworkDto>();

        var followerIds = network.Followers.Skip(query.Skip).Take(query.Take).ToList();
        var followers = new List<SocialNetworkDto>();

        foreach (var followerId in followerIds)
        {
            var followerNetwork = await _repository.GetByUserIdAsync(followerId, cancellationToken);
            if (followerNetwork != null)
            {
                followers.Add(new SocialNetworkDto(
                    followerNetwork.Id,
                    followerNetwork.UserId,
                    followerNetwork.Connections.Select(c => new SocialConnectionDto(c.Id, c.ConnectedUserId, c.Type, c.Note)).ToList(),
                    followerNetwork.Followers,
                    followerNetwork.Following,
                    new UserProfileDto(followerNetwork.Profile.Name, followerNetwork.Profile.Bio, followerNetwork.Profile.ProfileImageUrl, followerNetwork.Profile.Location),
                    followerNetwork.Posts.Select(p => new UserPostDto(p.Id, p.Content, p.Tags, p.Likes.Count, p.Comments.Count, p.CreatedAt)).ToList(),
                    followerNetwork.Followers.Count,
                    followerNetwork.Following.Count));
            }
        }

        return followers;
    }
}
