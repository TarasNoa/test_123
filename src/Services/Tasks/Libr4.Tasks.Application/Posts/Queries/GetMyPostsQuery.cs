using Libr4.Shared.Kernel.Results;
using Libr4.Tasks.Application.Posts.Dtos;
using MediatR;

namespace Libr4.Tasks.Application.Posts.Queries;

public sealed record GetMyPostsQuery(Guid UserId) : IRequest<Result<List<PostDto>>>;

public sealed class GetMyPostsHandler : IRequestHandler<GetMyPostsQuery, Result<List<PostDto>>>
{
    public async Task<Result<List<PostDto>>> Handle(GetMyPostsQuery request, CancellationToken ct)
    {
        return Result.Success(new List<PostDto>());
    }
}
