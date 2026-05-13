using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetProfileAnalyticsQuery : IQuery<ProfileAnalyticsDto>
{
    public Guid UserId { get; set; }
    public Guid CurrentUserId { get; set; }
}

public record ProfileAnalyticsDto(int PostCount, int FollowerCount, int FollowingCount, double EngagementRate);

public class GetProfileAnalyticsQueryHandler : IQueryHandler<GetProfileAnalyticsQuery, ProfileAnalyticsDto>
{
    private readonly ISocialNetworkRepository _repository;

    public GetProfileAnalyticsQueryHandler(ISocialNetworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProfileAnalyticsDto> HandleAsync(GetProfileAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (network == null)
            return new ProfileAnalyticsDto(0, 0, 0, 0);

        var postCount = network.Posts.Count;
        var followerCount = network.Followers.Count;
        var followingCount = network.Following.Count;
        var totalLikes = network.Posts.Sum(p => p.Likes.Count);
        var totalComments = network.Posts.Sum(p => p.Comments.Count);
        var engagementRate = postCount > 0 ? (double)(totalLikes + totalComments) / postCount : 0;

        return new ProfileAnalyticsDto(postCount, followerCount, followingCount, engagementRate);
    }
}
