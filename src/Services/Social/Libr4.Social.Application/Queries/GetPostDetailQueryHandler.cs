using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetPostDetailQuery : IQuery<PostDetailDto>
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
}

public class GetPostDetailQueryHandler : IQueryHandler<GetPostDetailQuery, PostDetailDto>
{
    private readonly ISocialNetworkRepository _repository;

    public GetPostDetailQueryHandler(ISocialNetworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<PostDetailDto> HandleAsync(GetPostDetailQuery query, CancellationToken cancellationToken)
    {
        var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (network == null)
            throw new InvalidOperationException("User network not found");

        var post = network.Posts.FirstOrDefault(p => p.Id == query.PostId);
        if (post == null)
            throw new InvalidOperationException("Post not found");

        return new PostDetailDto(
            post.Id,
            post.Content,
            post.Tags,
            post.Likes,
            post.Comments.Select(c => new PostCommentDto(c.Id, c.AuthorId, c.Text, c.CreatedAt)).ToList(),
            post.CreatedAt
        );
    }
}
