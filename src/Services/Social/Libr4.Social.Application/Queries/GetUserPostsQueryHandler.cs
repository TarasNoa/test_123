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
        var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (network == null)
            return new List<UserPostDto>();

        return network.Posts
            .OrderByDescending(p => p.CreatedAt)
            .Skip(query.Skip)
            .Take(query.Take)
            .Select(p => new UserPostDto(
                p.Id, 
                p.Content, 
                p.Tags,
                p.Likes.Count, 
                p.Comments.Count, 
                p.CreatedAt,
                p.Likes.Contains(query.UserId),
                p.Comments.Select(c => new PostCommentDto(c.Id, c.AuthorId, c.Text, c.CreatedAt)).ToList()))
            .ToList();
    }
}