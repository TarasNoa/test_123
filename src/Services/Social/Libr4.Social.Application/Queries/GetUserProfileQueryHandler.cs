using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Infrastructure.Caching;

namespace Libr4.Social.Application.Queries;

public class GetUserProfileQuery
{
    public Guid UserId { get; set; }
}

public class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ICacheService _cache;

    public GetUserProfileQueryHandler(ISocialNetworkRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery query)
    {
        var cacheKey = $"profile:{query.UserId}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var network = await _repository.GetByUserIdAsync(query.UserId);
            if (network == null)
                throw new InvalidOperationException("User not found");

            return new UserProfileDto(
                network.Profile.Name,
                network.Profile.Bio,
                network.Profile.ProfileImageUrl,
                network.Profile.Location
            );
        }, TimeSpan.FromHours(1));
    }
}