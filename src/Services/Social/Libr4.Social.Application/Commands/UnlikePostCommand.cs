using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class UnlikePostCommand
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
}

public class UnlikePostCommandHandler : ICommandHandler<UnlikePostCommand>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ILogger<UnlikePostCommandHandler> _logger;

    public UnlikePostCommandHandler(ISocialNetworkRepository repository, ILogger<UnlikePostCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(UnlikePostCommand command)
    {
        var network = await _repository.GetByUserIdAsync(command.UserId);
        if (network == null)
            throw new InvalidOperationException("User network not found");

        var post = network.Posts.FirstOrDefault(p => p.Id == command.PostId);
        if (post != null)
        {
            post.Likes.Remove(command.UserId);
            await _repository.UpdateAsync(network);
        }

        _logger.LogInformation("Post {PostId} unliked by user {UserId}", command.PostId, command.UserId);
    }
}
