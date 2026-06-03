using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Infrastructure.Caching;
using Libr4.Shared.Kernel.Application;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetUserProfileQuery : IQuery<UserPublicProfileDto>
{
    public Guid UserId { get; set; }
}

public class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserPublicProfileDto>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ICacheService _cache;

    public GetUserProfileQueryHandler(ISocialNetworkRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<UserPublicProfileDto> HandleAsync(GetUserProfileQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"profile:{query.UserId}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
            if (network == null)
                throw new InvalidOperationException("User not found");

            return new UserPublicProfileDto(
                network.Profile.Name,
                network.Profile.Bio,
                network.Profile.ProfileImageUrl,
                network.Profile.Location,
                network.Followers.Count,
                network.Following.Count,
                network.Posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new UserPostDto(p.Id, p.Content, p.Tags, p.Likes.Count, p.Comments.Count, p.CreatedAt))
                    .ToList()
            );
        }, TimeSpan.FromHours(1));
    }
}