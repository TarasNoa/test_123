using Libr4.Shared.Kernel.Results;
using MediatR;

namespace Libr4.Tasks.Application.Posts.Commands;

public sealed record CreatePostCommand(Guid UserId, string Content, string Title, List<string>? Tags, List<string>? MediaUrls) : IRequest<Result<Guid>>;

public sealed class CreatePostHandler : IRequestHandler<CreatePostCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePostCommand request, CancellationToken ct)
    {
        return Result.Success(Guid.NewGuid());
    }
}
