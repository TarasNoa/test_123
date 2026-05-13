using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Infrastructure.Caching;
using Libr4.Shared.Kernel.Application;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetUserPostsQuery : IQuery<List<UserPostDto>>
{
    public Guid UserId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
}

public class GetUserPostsQueryHandler : IQueryHandler<GetUserPostsQuery, List<UserPostDto>>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ICacheService _cache;

    public GetUserPostsQueryHandler(ISocialNetworkRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<List<UserPostDto>> HandleAsync(GetUserPostsQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"user_posts:{query.UserId}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
            if (network == null)
                return new List<UserPostDto>();

            return network.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Skip(query.Skip)
                .Take(query.Take)
                .Select(p => new UserPostDto(p.Id, p.Content, p.Tags, p.Likes.Count, p.Comments.Count, p.CreatedAt))
                .ToList();
        }, TimeSpan.FromMinutes(5));
    }
}