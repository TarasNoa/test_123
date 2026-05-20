using Libr4.Shared.Kernel.Results;
using MediatR;

namespace Libr4.Tasks.Application.Posts.Commands;

public sealed record AddCommentCommand(Guid PostId, Guid UserId, string Content) : IRequest<Result<Guid>>;

public sealed class AddCommentHandler : IRequestHandler<AddCommentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddCommentCommand request, CancellationToken ct)
    {
        return Result.Success(Guid.NewGuid());
    }
}
