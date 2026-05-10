using Libr4.Shared.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class SharePostCommand
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public string? PersonalMessage { get; set; }
}

public class SharePostCommandHandler : ICommandHandler<SharePostCommand>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<SharePostCommandHandler> _logger;

    public SharePostCommandHandler(
        ISocialNetworkRepository repository,
        IEventPublisher eventPublisher,
        ILogger<SharePostCommandHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(SharePostCommand command)
    {
        var network = await _repository.GetByUserIdAsync(command.UserId);
        if (network == null)
            throw new InvalidOperationException("User network not found");

        network.SharePost(command.PostId, command.PersonalMessage);
        await _repository.UpdateAsync(network);

        _logger.LogInformation($"Post {command.PostId} shared by user {command.UserId}");
    }
}