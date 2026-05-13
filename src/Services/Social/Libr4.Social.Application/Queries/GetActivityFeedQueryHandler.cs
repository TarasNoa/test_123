using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetActivityFeedQuery : IQuery<List<UserActivityDto>>
{
    public Guid UserId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

public class GetActivityFeedQueryHandler : IQueryHandler<GetActivityFeedQuery, List<UserActivityDto>>
{
    private readonly ISocialNetworkRepository _repository;

    public GetActivityFeedQueryHandler(ISocialNetworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<UserActivityDto>> HandleAsync(GetActivityFeedQuery query, CancellationToken cancellationToken)
    {
        var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (network == null)
            return new List<UserActivityDto>();

        return network.ActivityFeed
            .OrderByDescending(a => a.Timestamp)
            .Skip(query.Skip)
            .Take(query.Take)
            .Select(a => new UserActivityDto(a.Description, a.Timestamp, a.Type))
            .ToList();
    }
}
