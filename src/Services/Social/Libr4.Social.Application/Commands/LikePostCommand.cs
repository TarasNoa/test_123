using Libr4.Shared.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class LikePostCommand
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
}

public class LikePostCommandHandler : ICommandHandler<LikePostCommand>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<LikePostCommandHandler> _logger;

    public LikePostCommandHandler(
        ISocialNetworkRepository repository,
        IEventPublisher eventPublisher,
        ILogger<LikePostCommandHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(LikePostCommand command)
    {
        var network = await _repository.GetByUserIdAsync(command.UserId);
        if (network == null)
            throw new InvalidOperationException("User network not found");

        network.LikePost(command.PostId, command.UserId);
        await _repository.UpdateAsync(network);

        _logger.LogInformation($"Post {command.PostId} liked by user {command.UserId}");
    }
}