using Libr4.Shared.Kernel.Results;
using Libr4.Tasks.Application.Posts.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Tasks.Application.Posts.Queries;

public sealed record GetFeedQuery([FromQuery] int Page = 1, [FromQuery] int PageSize = 20) : IRequest<Result<List<PostDto>>>;

public sealed class GetFeedHandler : IRequestHandler<GetFeedQuery, Result<List<PostDto>>>
{
    public async Task<Result<List<PostDto>>> Handle(GetFeedQuery request, CancellationToken ct)
    {
        return Result.Success(new List<PostDto>());
    }
}
