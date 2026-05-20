using Libr4.Shared.Kernel.Results;
using MediatR;

namespace Libr4.Tasks.Application.Posts.Commands;

public sealed record LikePostCommand(Guid PostId, Guid UserId) : IRequest<Result>;

public sealed class LikePostHandler : IRequestHandler<LikePostCommand, Result>
{
    public async Task<Result> Handle(LikePostCommand request, CancellationToken ct)
    {
        return Result.Success();
    }
}
