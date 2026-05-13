using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Infrastructure.Caching;
using Libr4.Shared.Kernel.Application;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetFeedQuery : IQuery<List<UserPostDto>>
{
    public Guid UserId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
}

public class GetFeedQueryHandler : IQueryHandler<GetFeedQuery, List<UserPostDto>>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ICacheService _cache;

    public GetFeedQueryHandler(ISocialNetworkRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<List<UserPostDto>> HandleAsync(GetFeedQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"feed:{query.UserId}:{query.Skip}:{query.Take}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
            if (network == null)
                return new List<UserPostDto>();

            var feedPostIds = new List<(Guid Id, DateTime CreatedAt)>();

            // Add user's own posts
            foreach (var post in network.Posts)
            {
                feedPostIds.Add((post.Id, post.CreatedAt));
            }

            // Add posts from following
            foreach (var followingId in network.Following)
            {
                var followingNetwork = await _repository.GetByUserIdAsync(followingId, cancellationToken);
                if (followingNetwork != null)
                {
                    foreach (var post in followingNetwork.Posts)
                    {
                        feedPostIds.Add((post.Id, post.CreatedAt));
                    }
                }
            }

            return feedPostIds
                .OrderByDescending(x => x.CreatedAt)
                .Skip(query.Skip)
                .Take(query.Take)
                .Select(x => new UserPostDto(x.Id, "", new List<string>(), 0, 0, x.CreatedAt))
                .ToList();
        }, TimeSpan.FromMinutes(5));
    }
}