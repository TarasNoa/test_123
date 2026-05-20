using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetRecommendedConnectionsQuery : IQuery<List<RecommendedUserDto>>
{
    public Guid UserId { get; set; }
    public int TopN { get; set; } = 10;
}

public class GetRecommendedConnectionsQueryHandler : IQueryHandler<GetRecommendedConnectionsQuery, List<RecommendedUserDto>>
{
    private readonly ISocialNetworkRepository _repository;

    public GetRecommendedConnectionsQueryHandler(ISocialNetworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<RecommendedUserDto>> HandleAsync(GetRecommendedConnectionsQuery query, CancellationToken cancellationToken)
    {
        var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (network == null)
            return new List<RecommendedUserDto>();

        var allNetworks = await _repository.GetAllAsync(cancellationToken);
        var recommendations = allNetworks
            .Where(n => n.UserId != query.UserId && !network.Connections.Any(c => c.ConnectedUserId == n.UserId))
            .OrderByDescending(n => n.Followers.Count)
            .Take(query.TopN)
            .Select(n => new RecommendedUserDto(
                n.UserId,
                n.Profile.Name,
                $"@{n.Profile.Name.ToLowerInvariant().Replace(' ', '_')}",
                network.Following.Contains(n.UserId)))
            .ToList();

        return recommendations;
    }
}
