using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;
using Libr4.Social.Domain.Network;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class UnfollowUserCommand
{
    public Guid UserId { get; set; }
    public Guid TargetUserId { get; set; }
}

public class UnfollowUserCommandHandler : ICommandHandler<UnfollowUserCommand>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ILogger<UnfollowUserCommandHandler> _logger;

    public UnfollowUserCommandHandler(ISocialNetworkRepository repository, ILogger<UnfollowUserCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(UnfollowUserCommand command)
    {
        var userNetwork = await _repository.GetByUserIdAsync(command.UserId);
        if (userNetwork == null)
            throw new InvalidOperationException("User network not found");

        var targetNetwork = await _repository.GetByUserIdAsync(command.TargetUserId);
        if (targetNetwork == null)
            throw new InvalidOperationException("Target user network not found");

        userNetwork.RemoveConnection(command.TargetUserId);
        targetNetwork.RemoveFollower(command.UserId);

        await _repository.UpdateAsync(userNetwork);
        await _repository.UpdateAsync(targetNetwork);

        _logger.LogInformation("User {UserId} unfollowed {TargetUserId}", command.UserId, command.TargetUserId);
    }
}
