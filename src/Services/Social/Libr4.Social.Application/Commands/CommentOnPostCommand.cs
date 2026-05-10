using Libr4.Shared.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class CommentOnPostCommand
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public string CommentText { get; set; } = string.Empty;
}

public class CommentOnPostCommandHandler : ICommandHandler<CommentOnPostCommand>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CommentOnPostCommandHandler> _logger;

    public CommentOnPostCommandHandler(
        ISocialNetworkRepository repository,
        IEventPublisher eventPublisher,
        ILogger<CommentOnPostCommandHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(CommentOnPostCommand command)
    {
        var network = await _repository.GetByUserIdAsync(command.UserId);
        if (network == null)
            throw new InvalidOperationException("User network not found");

        network.CommentOnPost(command.PostId, command.UserId, command.CommentText);
        await _repository.UpdateAsync(network);

        _logger.LogInformation($"Post {command.PostId} commented by user {command.UserId}");
    }
}