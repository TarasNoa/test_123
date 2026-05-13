using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetPostsAnalyticsQuery : IQuery<PostsAnalyticsDto>
{
    public Guid UserId { get; set; }
}

public record PostsAnalyticsDto(int TotalPosts, int TotalLikes, int TotalComments, int TotalShares, double AverageEngagement);

public class GetPostsAnalyticsQueryHandler : IQueryHandler<GetPostsAnalyticsQuery, PostsAnalyticsDto>
{
    private readonly ISocialNetworkRepository _repository;

    public GetPostsAnalyticsQueryHandler(ISocialNetworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<PostsAnalyticsDto> HandleAsync(GetPostsAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (network == null)
            return new PostsAnalyticsDto(0, 0, 0, 0, 0);

        var totalPosts = network.Posts.Count;
        var totalLikes = network.Posts.Sum(p => p.Likes.Count);
        var totalComments = network.Posts.Sum(p => p.Comments.Count);
        var totalShares = network.Posts.Sum(p => p.Shares.Count);
        var averageEngagement = totalPosts > 0 ? (double)(totalLikes + totalComments + totalShares) / totalPosts : 0;

        return new PostsAnalyticsDto(totalPosts, totalLikes, totalComments, totalShares, averageEngagement);
    }
}
