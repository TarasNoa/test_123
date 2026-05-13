using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetRecommendedConnectionsQuery : IQuery<List<SocialNetworkDto>>
{
    public Guid UserId { get; set; }
    public int TopN { get; set; } = 10;
}

public class GetRecommendedConnectionsQueryHandler : IQueryHandler<GetRecommendedConnectionsQuery, List<SocialNetworkDto>>
{
    private readonly ISocialNetworkRepository _repository;

    public GetRecommendedConnectionsQueryHandler(ISocialNetworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SocialNetworkDto>> HandleAsync(GetRecommendedConnectionsQuery query, CancellationToken cancellationToken)
    {
        var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (network == null)
            return new List<SocialNetworkDto>();

        var allNetworks = await _repository.GetAllAsync(cancellationToken);
        var recommendations = allNetworks
            .Where(n => n.UserId != query.UserId && !network.Connections.Any(c => c.ConnectedUserId == n.UserId))
            .OrderByDescending(n => n.Followers.Count)
            .Take(query.TopN)
            .Select(n => new SocialNetworkDto(
                n.Id,
                n.UserId,
                n.Connections.Select(c => new SocialConnectionDto(c.Id, c.ConnectedUserId, c.Type, c.Note)).ToList(),
                n.Followers,
                n.Following,
                new UserProfileDto(n.Profile.Name, n.Profile.Bio, n.Profile.ProfileImageUrl, n.Profile.Location),
                n.Posts.Select(p => new UserPostDto(p.Id, p.Content, p.Tags, p.Likes.Count, p.Comments.Count, p.CreatedAt)).ToList(),
                n.Followers.Count,
                n.Following.Count))
            .ToList();

        return recommendations;
    }
}
