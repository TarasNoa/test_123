using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Infrastructure.Events;
using Libr4.Social.Application.Abstractions;
using Libr4.Social.Domain.Network;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class FollowUserCommand
{
    public Guid UserId { get; set; }
    public Guid TargetUserId { get; set; }
}

public class FollowUserCommandHandler : ICommandHandler<FollowUserCommand>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<FollowUserCommandHandler> _logger;

    public FollowUserCommandHandler(
        ISocialNetworkRepository repository,
        IEventPublisher eventPublisher,
        ILogger<FollowUserCommandHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(FollowUserCommand command)
    {
        var userNetwork = await _repository.GetByUserIdAsync(command.UserId);
        if (userNetwork == null)
            throw new InvalidOperationException("User network not found");

        var targetNetwork = await _repository.GetByUserIdAsync(command.TargetUserId);
        if (targetNetwork == null)
            throw new InvalidOperationException("Target user network not found");

        userNetwork.AddConnection(command.TargetUserId, ConnectionType.Following);
        targetNetwork.AddFollower(command.UserId);

        await _repository.UpdateAsync(userNetwork);
        await _repository.UpdateAsync(targetNetwork);

        _logger.LogInformation($"User {command.UserId} followed {command.TargetUserId}");
    }
}